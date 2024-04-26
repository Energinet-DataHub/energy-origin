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
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementProposalCleanup;
using Asp.Versioning.ApiExplorer;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.ActivityLog.HostedService;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Testcontainers.PostgreSql;
using WireMock.Server;
using Xunit;

namespace API.IntegrationTests.Factories;

public class TransferAgreementsApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; set; } = "";

    private string WalletUrl { get; set; } = "http://foo";

    private byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

    private string OtlpReceiverEndpoint { get; set; } = "http://foo";

    private const string CvrUser = "SomeUser";
    private const string CvrPassword = "SomePassword";
    public string CvrBaseUrl { get; set; } = "SomeUrl";
    public bool WithCleanupWorker { get; set; } = true;

    public IProjectOriginWalletService WalletServiceMock { get; private set; } = Substitute.For<IProjectOriginWalletService>();

    public async Task WithApiVersionDescriptionProvider(Func<IApiVersionDescriptionProvider, Task> withAction)
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        await withAction(provider);
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

        builder.ConfigureTestServices(s =>
        {
            s.Configure<ActivityLogOptions>(options =>
            {
                options.ServiceName = "transfer";
                options.CleanupActivityLogsOlderThanInDays = -1;
                options.CleanupIntervalInSeconds = 3;
            });

            s.Configure<DatabaseOptions>(o =>
            {
                var connectionStringBuilder = new DbConnectionStringBuilder
                {
                    ConnectionString = ConnectionString
                };
                o.Host = (string)connectionStringBuilder["Host"];
                o.Port = (string)connectionStringBuilder["Port"];
                o.Name = (string)connectionStringBuilder["Database"];
                o.User = (string)connectionStringBuilder["Username"];
                o.Password = (string)connectionStringBuilder["Password"];
            });

            s.Remove(s.First(sd => sd.ImplementationType == typeof(ProjectOriginWalletService)));
            s.AddScoped(_ => WalletServiceMock);

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
        using var serviceScope = host.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        return host;
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

    public void Start() => Server.Should().NotBeNull();
}
