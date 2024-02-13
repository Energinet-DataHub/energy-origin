using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning.ApiExplorer;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Testing.Testcontainers;
using TransferAgreementAutomation.Worker;
using Xunit;

namespace Worker.IntegrationTest;

public class TransferAutomationApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainer postgresContainer = new();

    private readonly byte[] privateKey = RsaKeyGenerator.GenerateTestKey();
    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = "20230101")
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
        builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
        builder.UseSetting("TokenValidation:Audience", "Users");
        builder.UseSetting("TransferApi:Url", "localhost:5001");
        builder.UseSetting("TransferApi:Version", "20231123");

        var connectionStringBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = postgresContainer.ConnectionString
        };
        builder.UseSetting("Database:Host", (string)connectionStringBuilder["Host"]);
        builder.UseSetting("Database:Port", (string)connectionStringBuilder["Port"]);
        builder.UseSetting("Database:Name", (string)connectionStringBuilder["Database"]);
        builder.UseSetting("Database:User", (string)connectionStringBuilder["Username"]);
        builder.UseSetting("Database:Password", (string)connectionStringBuilder["Password"]);

        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.First(s => s.ImplementationType == typeof(TransferAgreementsAutomationWorker)));
        });
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

    public Task InitializeAsync() => postgresContainer.InitializeAsync();

    Task IAsyncLifetime.DisposeAsync() => postgresContainer.DisposeAsync();
}
