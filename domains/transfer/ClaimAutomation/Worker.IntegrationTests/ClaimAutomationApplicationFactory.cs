using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning.ApiExplorer;
using ClaimAutomation.Worker;
using ClaimAutomation.Worker.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Testcontainers.PostgreSql;
using Xunit;

namespace Worker.IntegrationTests;

public class ClaimAutomationApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer = new PostgreSqlBuilder().WithImage("postgres:15.2").Build();

    public Task InitializeAsync() => testContainer.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => testContainer.DisposeAsync().AsTask();

    private readonly byte[] privateKey = RsaKeyGenerator.GenerateTestKey();
    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = "20231123")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);

        return client;
    }
    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("EO_API_VERSION", "20231123");
        return client;
    }

    public IApiVersionDescriptionProvider GetApiVersionDescriptionProvider()
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        return provider;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var privateKeyPem = Encoding.UTF8.GetString(privateKey);
        string publicKeyPem;

        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKeyPem);
            publicKeyPem = rsa.ExportRSAPublicKeyPem();
        }

        var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

        builder.UseSetting("ProjectOrigin:WalletUrl", "http://localhost:5000");
        builder.UseSetting("Otlp:ReceiverEndpoint", "http://foobar");
        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "Us");
        builder.UseSetting("TokenValidation:Audience", "Users");

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
    private string GenerateToken(
        string scope = "",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
        string sub = "03bad0af-caeb-46e8-809c-1d35a5863bc7",
        string tin = "11223344",
        string cpn = "Producent A/S",
        string name = "Peter Producent",
        string issuer = "Us",
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
            { UserClaimName.ProviderType, ProviderType.MitIdProfessional.ToString() },
            { UserClaimName.AllowCprLookup, "false"},
            { UserClaimName.AccessToken, ""},
            { UserClaimName.IdentityToken, ""},
            { UserClaimName.ProviderKeys, ""},
            { UserClaimName.OrganizationId, sub},
            { UserClaimName.MatchedRoles, ""},
            { UserClaimName.Roles, ""},
            { UserClaimName.AssignedRoles, ""}
        };
        return new TokenSigner(privateKey).Sign(
            sub,
            name,
            issuer,
            audience,
            null,
            60,
            claims
        );
    }
}
