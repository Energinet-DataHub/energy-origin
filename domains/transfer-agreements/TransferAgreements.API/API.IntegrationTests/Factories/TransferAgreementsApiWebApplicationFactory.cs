using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Org.BouncyCastle.Crypto.Agreement;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Factories;

public class TransferAgreementsApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer = new PostgreSqlBuilder().WithImage("postgres:15.2").Build();

    public Task InitializeAsync() => testContainer.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => testContainer.DisposeAsync().AsTask();

    public string WalletUrl { get; set; }

    public string OtlpReceiverEndpoint { get; set; } = "http://foo";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Otlp:ReceiverEndpoint", OtlpReceiverEndpoint);

        builder.ConfigureTestServices(s =>
        {
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

            s.Configure<ProjectOriginOptions>(o => o.WalletUrl = WalletUrl);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        var serviceScope = host.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        return host;
    }

    public async Task SeedData(IEnumerable<TransferAgreement> transferAgreements)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await TruncateTables(dbContext);

        foreach (var agreement in transferAgreements)
        {
            await InsertTransferAgreement(dbContext, agreement);
            await InsertHistoryEntry(dbContext, agreement);
        }
    }

    public async Task SeedConnections(IEnumerable<Connection> connections)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await TruncateTables(dbContext);

        foreach (var connection in connections)
        {
            await InsertConnection(dbContext, connection);
        }
    }

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent", string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name, actor: actor));

        return client;
    }

    private static string GenerateToken(
        string scope = "",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
        string sub = "03bad0af-caeb-46e8-809c-1d35a5863bc7",
        string tin = "11223344",
        string cpn = "Producent A/S",
        string name = "Peter Producent",
        string issuer = "DkTest1",
        string audience = "Users")
    {
        var key = Encoding.ASCII.GetBytes("TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST");

        var claims = new[]
        {
            new Claim("sub", sub),
            new Claim("scope", scope),
            new Claim("actor", actor),
            new Claim("atr", actor),
            new Claim("tin", tin),
            new Claim("cpn", cpn),
            new Claim("name", name),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("iss", issuer),
            new Claim("aud", audience)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(1),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static async Task TruncateTables(ApplicationDbContext dbContext)
    {
        var historyTable = dbContext.Model.FindEntityType(typeof(TransferAgreementHistoryEntry)).GetTableName();
        var agreementsTable = dbContext.Model.FindEntityType(typeof(TransferAgreement)).GetTableName();
        var connectionsTable = dbContext.Model.FindEntityType(typeof(Connection)).GetTableName();

        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{historyTable}\"");
        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{agreementsTable}\" CASCADE");
        await dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{connectionsTable}\"");
    }

    private static async Task InsertConnection(ApplicationDbContext dbContext, Connection connection)
    {
        var connectionTable = dbContext.Model.FindEntityType(typeof(Connection)).GetTableName();

        var agreementQuery = $"INSERT INTO \"{connectionTable}\" (\"Id\", \"CompanyId\", \"CompanyTin\", \"OwnerId\") VALUES (@Id, @CompanyId, @CompanyTin, @OwnerId)";
        var agreementFields = new[]
        {
            new NpgsqlParameter("Id", connection.Id),
            new NpgsqlParameter("CompanyId", connection.CompanyId),
            new NpgsqlParameter("CompanyTin", connection.CompanyTin),
            new NpgsqlParameter("OwnerId", connection.OwnerId)
        };

        await dbContext.Database.ExecuteSqlRawAsync(agreementQuery, agreementFields);
    }

    private static async Task InsertTransferAgreement(ApplicationDbContext dbContext, TransferAgreement agreement)
    {
        var agreementsTable = dbContext.Model.FindEntityType(typeof(TransferAgreement)).GetTableName();

        var agreementQuery = $"INSERT INTO \"{agreementsTable}\" (\"Id\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\", \"ReceiverReference\") VALUES (@Id, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin, @ReceiverReference)";
        var agreementFields = new[]
        {
            new NpgsqlParameter("Id", agreement.Id),
            new NpgsqlParameter("StartDate", agreement.StartDate),
            new NpgsqlParameter("EndDate", agreement.EndDate),
            new NpgsqlParameter("SenderId", agreement.SenderId),
            new NpgsqlParameter("SenderName", agreement.SenderName),
            new NpgsqlParameter("SenderTin", agreement.SenderTin),
            new NpgsqlParameter("ReceiverTin", agreement.ReceiverTin),
            new NpgsqlParameter("ReceiverReference", agreement.ReceiverReference)
        };

        await dbContext.Database.ExecuteSqlRawAsync(agreementQuery, agreementFields);
    }

    private static async Task InsertHistoryEntry(ApplicationDbContext dbContext, TransferAgreement agreement)
    {
        var historyTable = dbContext.Model.FindEntityType(typeof(TransferAgreementHistoryEntry)).GetTableName();

        var historyQuery = $"INSERT INTO \"{historyTable}\" (\"Id\", \"CreatedAt\", \"AuditAction\", \"ActorId\", \"ActorName\", \"TransferAgreementId\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\") " +
                           "VALUES (@Id, @CreatedAt, @AuditAction, @ActorId, @ActorName, @TransferAgreementId, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin)";
        var historyFields = new[]
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
