using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.IntegrationTests.Mocks;
using API.MeasurementsSyncer;
using API.Query.API.Controllers;
using Asp.Versioning.ApiExplorer;
using Contracts;
using DataContext;
using DataContext.ValueObjects;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests.Factories;

public class QueryApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly List<GrpcChannel> disposableChannels = new();

    public string ConnectionString { get; set; } = "";
    public string MeasurementsUrl { get; set; } = "http://foo";
    public string WalletUrl { get; set; } = "bar";

    private string OtlpReceiverEndpoint { get; set; } = "http://foo";
    public RabbitMqOptions? RabbitMqOptions { get; set; }
    private byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

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
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("Measurements:Url", MeasurementsUrl);
        builder.UseSetting("Measurements:GrpcUrl", "http://foo");
        builder.UseSetting("MeasurementsSync:Disabled", "false");
        builder.UseSetting("IssuingContractCleanup:SleepTime", "00:00:03");
        builder.UseSetting("Wallet:Url", WalletUrl);
        builder.UseSetting("RabbitMq:Password", RabbitMqOptions?.Password ?? "");
        builder.UseSetting("RabbitMq:Username", RabbitMqOptions?.Username ?? "");
        builder.UseSetting("RabbitMq:Host", RabbitMqOptions?.Host ?? "localhost");
        builder.UseSetting("RabbitMq:Port", RabbitMqOptions?.Port.ToString() ?? "4242");
        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
        builder.UseSetting("TokenValidation:Audience", "Users");

        builder.ConfigureTestServices(services =>
        {
            services.Configure<ActivityLogOptions>(options =>
            {
                options.ServiceName = "certificates";
                options.CleanupActivityLogsOlderThanInDays = -1;
                options.CleanupIntervalInSeconds = 3;
            });

            services.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(5);
                // Ensure masstransit bus is started when we run our health checks
                options.WaitUntilStarted = RabbitMqOptions != null;
            });

            // Remove MeasurementsSyncerWorker
            services.Remove(services.First(s => s.ImplementationType == typeof(MeasurementsSyncerWorker)));
        });
    }

    public async Task WithApiVersionDescriptionProvider(Func<IApiVersionDescriptionProvider, Task> withAction)
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        await withAction(provider);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return host;

        var factory = host.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        using var dbContext = factory.CreateDbContext();
        dbContext.Database.Migrate();

        return host;
    }

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = ApiVersions.Version20230101)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);

        return client;
    }

    public HttpClient CreateWalletClient(string subject)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(WalletUrl);
        var authentication = new AuthenticationHeaderValue("Bearer", GenerateToken(sub: subject));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authentication.Scheme, authentication.Parameter);

        return client;
    }

    public IBus GetMassTransitBus() => Services.GetRequiredService<IBus>();

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

    public async Task AddContract(string subject,
        string gsrn,
        DateTimeOffset startDate,
        MeteringPointType meteringPointType,
        MeasurementsWireMock measurementsWireMock,
        Technology technology = null!)
    {
        measurementsWireMock.SetupMeteringPointsResponse(gsrn: gsrn, type: meteringPointType, technology: technology);

        using var client = CreateAuthenticatedClient(subject);
        var body = new { gsrn, startDate = startDate.ToUnixTimeSeconds() };
        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public void Start() => Server.Should().NotBeNull();

    protected override void Dispose(bool disposing)
    {
        foreach (var disposableChannel in disposableChannels)
        {
            disposableChannel.Dispose();
        }

        base.Dispose(disposing);
    }
}
