using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.MeasurementsSyncer;
using Asp.Versioning.ApiExplorer;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementProposalCleanup;
using Contracts;
using DataContext;
using DataContext.Models;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.ActivityLog.HostedService;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Factories;

public class TransferAgreementsApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer = new PostgreSqlBuilder().WithImage("postgres:15.2").Build();

    private readonly List<GrpcChannel> disposableChannels = new();
    public RabbitMqOptions? RabbitMqOptions { get; set; }

    public string ConnectionString { get; set; } = "";
    public string MeasurementsUrl { get; set; } = "http://foo";

    public Task InitializeAsync() => testContainer.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => testContainer.DisposeAsync().AsTask();

    private string WalletUrl { get; set; } = "http://foo";

    private byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

    private string OtlpReceiverEndpoint { get; set; } = "http://foo";

    private const string CvrUser = "SomeUser";
    private const string CvrPassword = "SomePassword";
    public string CvrBaseUrl { get; set; } = "SomeUrl";
    public bool WithCleanupWorker { get; set; } = true;

    public IApiVersionDescriptionProvider GetApiVersionDescriptionProvider()
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        return provider;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var privateKeyPem = Encoding.UTF8.GetString(PrivateKey);
        string publicKeyPem;

        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKeyPem);
            publicKeyPem = rsa.ExportRSAPublicKeyPem();
        }

        var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

        builder.UseSetting("Otlp:ReceiverEndpoint", OtlpReceiverEndpoint);
        builder.UseSetting("TransferAgreementProposalCleanupService:SleepTime", "00:00:03");
        builder.UseSetting("TransferAgreementCleanup:SleepTime", "00:00:03");
        builder.UseSetting("Cvr:BaseUrl", CvrBaseUrl);
        builder.UseSetting("Cvr:User", CvrUser);
        builder.UseSetting("Cvr:Password", CvrPassword);
        builder.UseSetting("ProjectOrigin:WalletUrl", WalletUrl);
        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
        builder.UseSetting("TokenValidation:Audience", "Users");

        // Certificate
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("Measurements:Url", MeasurementsUrl);
        builder.UseSetting("Measurements:GrpcUrl", "http://foo");
        builder.UseSetting("MeasurementsSync:Disabled", "false");
        builder.UseSetting("IssuingContractCleanup:SleepTime", "00:00:03");
        builder.UseSetting("RabbitMq:Password", RabbitMqOptions?.Password ?? "");
        builder.UseSetting("RabbitMq:Username", RabbitMqOptions?.Username ?? "");
        builder.UseSetting("RabbitMq:Host", RabbitMqOptions?.Host ?? "localhost");
        builder.UseSetting("RabbitMq:Port", RabbitMqOptions?.Port.ToString() ?? "4242");

        builder.ConfigureTestServices(s =>
        {
            s.Configure<ActivityLogOptions>(options =>
            {
                options.ServiceName = "transfer";
                options.CleanupActivityLogsOlderThanInDays = -1;
                options.CleanupIntervalInSeconds = 3;
            });

            s.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(5);
                // Ensure masstransit bus is started when we run our health checks
                options.WaitUntilStarted = RabbitMqOptions != null;
            });

            // Remove MeasurementsSyncerWorker
            s.Remove(s.First(s => s.ImplementationType == typeof(MeasurementsSyncerWorker)));


            s.Configure<DatabaseOptions>(o =>
            {
                var connectionStringBuilder = new DbConnectionStringBuilder
                {
                    ConnectionString = testContainer.GetConnectionString()
                };
                o.Host = (string)connectionStringBuilder["Host"];
                o.Port = (string)connectionStringBuilder["Port"];
                o.Name = (string)connectionStringBuilder["Database"];
                o.User = (string)connectionStringBuilder["Username"];
                o.Password = (string)connectionStringBuilder["Password"];
            });

            if (!WithCleanupWorker)
            {
                s.Remove(s.First(x => x.ImplementationType == typeof(TransferAgreementProposalCleanupWorker)));
                s.Remove(s.First(x => x.ImplementationType == typeof(TransferAgreementProposalCleanupService)));
                s.Remove(s.First(x => x.ImplementationType == typeof(CleanupActivityLogsHostedService)));
            }
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        var serviceScope = host.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<TransferDbContext>();
        dbContext.Database.Migrate();

        return host;
    }

    public async Task SeedTransferAgreements(IEnumerable<TransferAgreement> transferAgreements)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();
        await dbContext.TruncateTransferAgreementsTables();

        foreach (var agreement in transferAgreements)
        {
            await InsertTransferAgreement(dbContext, agreement);
            await InsertTransferAgreementHistoryEntry(dbContext, agreement);
        }
    }

    public async Task SeedTransferAgreementsSaveChangesAsync(TransferAgreement transferAgreement)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();
        dbContext.TransferAgreements.Add(transferAgreement);
        await dbContext.SaveChangesAsync();
    }

    public async Task SeedTransferAgreementProposals(IEnumerable<TransferAgreementProposal> proposals)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        foreach (var proposal in proposals)
        {
            dbContext.TransferAgreementProposals.Add(proposal);
        }

        await dbContext.SaveChangesAsync();
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("EO_API_VERSION", "20240103");
        return client;
    }

    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string cpn = "Producent A/S", string apiVersion = "20240103")
    {
        var client = CreateClient();
        AuthenticateHttpClient(client, sub: sub, tin: tin, name, actor, cpn, apiVersion: apiVersion);
        return client;
    }

    private HttpClient AuthenticateHttpClient(HttpClient client, string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string cpn = "Producent A/S", string apiVersion = "20240103")
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name, actor: actor, cpn: cpn));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);

        return client;
    }

    public HttpClient CreateAuthenticatedClient(IProjectOriginWalletService poWalletServiceMock, string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string cpn = "Peter Producent A/S", string apiVersion = "20240103")
    {
        var client = WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Remove(services.First(s => s.ImplementationType == typeof(ProjectOriginWalletService)));
                services.AddScoped(_ => poWalletServiceMock);
            });
        }).CreateClient();
        AuthenticateHttpClient(client, sub: sub, tin: tin, name, actor, cpn: cpn, apiVersion);
        return client;
    }

    private string GenerateToken(
        string scope = "",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
        string sub = "03bad0af-caeb-46e8-809c-1d35a5863bc7",
        string tin = "11223344",
        string cpn = "Producent A/S",
        string name = "Peter Producent",
        string issuer = "demo.energioprindelse.dk",
        string audience = "Users")
    {

        var claims = new Dictionary<string, object>()
        {
            { UserClaimName.Scope, scope },
            { UserClaimName.ActorLegacy, actor },
            { UserClaimName.Actor, actor },
            { UserClaimName.Tin, tin },
            { UserClaimName.OrganizationName, cpn },
            { JwtRegisteredClaimNames.Name, name },
            { UserClaimName.ProviderType, ProviderType.MitIdProfessional.ToString()},
            { UserClaimName.AllowCprLookup, "false"},
            { UserClaimName.AccessToken, ""},
            { UserClaimName.IdentityToken, ""},
            { UserClaimName.ProviderKeys, ""},
            { UserClaimName.OrganizationId, sub},
            { UserClaimName.MatchedRoles, ""},
            { UserClaimName.Roles, ""},
            { UserClaimName.AssignedRoles, ""}
        };

        var signedJwtToken = new TokenSigner(PrivateKey).Sign(
            sub,
            name,
            issuer,
            audience,
            null,
            60,
            claims
        );

        return signedJwtToken;
    }

    private static async Task InsertTransferAgreement(TransferDbContext dbContext, TransferAgreement agreement)
    {
        var agreementsTable = dbContext.Model.FindEntityType(typeof(TransferAgreement))!.GetTableName();

        var agreementQuery =
            $"INSERT INTO \"{agreementsTable}\" (\"Id\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\", \"ReceiverReference\", \"TransferAgreementNumber\") VALUES (@Id, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin, @ReceiverReference, @TransferAgreementNumber)";
        object[] agreementFields =
        {
            new NpgsqlParameter("Id", agreement.Id),
            new NpgsqlParameter("StartDate", agreement.StartDate),
            new NpgsqlParameter("EndDate", agreement.EndDate),
            new NpgsqlParameter("SenderId", agreement.SenderId),
            new NpgsqlParameter("SenderName", agreement.SenderName),
            new NpgsqlParameter("SenderTin", agreement.SenderTin),
            new NpgsqlParameter("ReceiverTin", agreement.ReceiverTin),
            new NpgsqlParameter("ReceiverReference", agreement.ReceiverReference),
            new NpgsqlParameter("TransferAgreementNumber", agreement.TransferAgreementNumber)
        };

        await dbContext.Database.ExecuteSqlRawAsync(agreementQuery, agreementFields);
    }

    private static async Task InsertTransferAgreementHistoryEntry(TransferDbContext dbContext, TransferAgreement agreement)
    {
        var historyTable = dbContext.Model.FindEntityType(typeof(TransferAgreementHistoryEntry))!.GetTableName();

        var historyQuery =
            $"INSERT INTO \"{historyTable}\" (\"Id\", \"CreatedAt\", \"AuditAction\", \"ActorId\", \"ActorName\", \"TransferAgreementId\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\") " +
            "VALUES (@Id, @CreatedAt, @AuditAction, @ActorId, @ActorName, @TransferAgreementId, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin)";
        object[] historyFields =
        {
            new NpgsqlParameter("Id", Guid.NewGuid()),
            new NpgsqlParameter("CreatedAt", DateTime.UtcNow),
            new NpgsqlParameter("AuditAction", "Insert"),
            new NpgsqlParameter("ActorId", "Test"),
            new NpgsqlParameter("ActorName", "Test"),
            new NpgsqlParameter("TransferAgreementId", agreement.Id),
            new NpgsqlParameter("StartDate", agreement.StartDate),
            new NpgsqlParameter("EndDate", agreement.EndDate),
            new NpgsqlParameter("SenderId", agreement.SenderId),
            new NpgsqlParameter("SenderName", agreement.SenderName),
            new NpgsqlParameter("SenderTin", agreement.SenderTin),
            new NpgsqlParameter("ReceiverTin", agreement.ReceiverTin)
        };

        await dbContext.Database.ExecuteSqlRawAsync(historyQuery, historyFields);
    }
}
