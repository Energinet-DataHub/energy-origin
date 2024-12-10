using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.Transfer.Api.Clients;
using API.Transfer.TransferAgreementProposalCleanup;
using Asp.Versioning.ApiExplorer;
using DataContext;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.ActivityLog.HostedService;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ProjectOriginClients;
using AuthenticationScheme = EnergyOrigin.TokenValidation.b2c.AuthenticationScheme;

namespace API.IntegrationTests.Factories;

public class TransferAgreementsApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; set; } = "";

    private string WalletUrl { get; set; } = "http://foo";

    private byte[] B2CDummyPrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

    private string OtlpReceiverEndpoint { get; set; } = "http://foo";

    private const string CvrUser = "SomeUser";
    private const string CvrPassword = "SomePassword";
    public string CvrBaseUrl { get; set; } = "SomeUrl";
    public bool WithCleanupWorker { get; set; } = true;
    public IProjectOriginWalletClient WalletClientMock { get; private set; } = Substitute.For<IProjectOriginWalletClient>();

    public async Task WithApiVersionDescriptionProvider(Func<IApiVersionDescriptionProvider, Task> withAction)
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        await withAction(provider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Otlp:ReceiverEndpoint", OtlpReceiverEndpoint);
        builder.UseSetting("TransferAgreementProposalCleanupService:SleepTime", "00:00:03");
        builder.UseSetting("TransferAgreementCleanup:SleepTime", "00:00:03");
        builder.UseSetting("Cvr:BaseUrl", CvrBaseUrl);
        builder.UseSetting("Cvr:User", CvrUser);
        builder.UseSetting("Cvr:Password", CvrPassword);
        builder.UseSetting("ProjectOrigin:WalletUrl", WalletUrl);

        builder.UseSetting("B2C:B2CWellKnownUrl",
            "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0/.well-known/openid-configuration");
        builder.UseSetting("B2C:ClientCredentialsCustomPolicyWellKnownUrl",
            "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_CLIENTCREDENTIALS");
        builder.UseSetting("B2C:MitIDCustomPolicyWellKnownUrl",
            "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_MITID");
        builder.UseSetting("B2C:Audience", "f00b9b4d-3c59-4c40-b209-2ef87e509f54");
        builder.UseSetting("B2C:CustomPolicyClientId", "a701d13c-2570-46fa-9aa2-8d81f0d8d60b");

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

            s.Remove(s.First(sd => sd.ServiceType == typeof(IProjectOriginWalletClient)));
            s.AddScoped(_ => WalletClientMock);

            if (!WithCleanupWorker)
            {
                s.Remove(s.First(x => x.ImplementationType == typeof(TransferAgreementProposalCleanupWorker)));
                s.Remove(s.First(x => x.ImplementationType == typeof(TransferAgreementProposalCleanupService)));
                s.Remove(s.First(x => x.ImplementationType == typeof(CleanupActivityLogsHostedService)));
            }

            s.Remove(s.First(sd => sd.ServiceType == typeof(IAuthorizationClient)));
            s.AddSingleton<IAuthorizationClient, MockAuthorizationClient>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var serviceScope = host.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        ReplaceB2CAuthenticationSchemes(host);

        return host;
    }

    private static void ReplaceB2CAuthenticationSchemes(IHost host)
    {
        var authenticationSchemeProvider = host.Services.GetService<IAuthenticationSchemeProvider>()!;
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme);

        var b2CScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CAuthenticationScheme,
            AuthenticationScheme.B2CAuthenticationScheme,
            typeof(IntegrationTestB2CAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CScheme);

        var b2CMitIdScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme,
            AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme,
            typeof(IntegrationTestB2CAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CMitIdScheme);

        var b2CClientCredentialsScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            typeof(IntegrationTestB2CAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CClientCredentialsScheme);
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);
        return client;
    }

    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string cpn = "Producent A/S", string apiVersion = ApiVersions.Version1)
    {
        var client = CreateClient();
        AuthenticateHttpClient(client, sub: sub, tin: tin, name, actor, cpn, apiVersion: apiVersion);
        return client;
    }


    private HttpClient AuthenticateHttpClient(HttpClient client, string sub, string tin = "11223344", string name = "Peter Producent",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string cpn = "Producent A/S", string apiVersion = ApiVersions.Version1)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateB2CDummyToken(sub: sub, tin: tin, name: name, cpn: cpn));
        client.DefaultRequestHeaders.Add("X-API-Version", apiVersion);

        return client;
    }

    public HttpClient CreateB2CAuthenticatedClient(Guid sub, Guid orgId, string tin = "11223344", string orgIds = "", string name = "Peter Producent",
        string apiVersion = ApiVersions.Version1, bool termsAccepted = true)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                GenerateB2CDummyToken(sub: sub.ToString(), tin: tin, name: name, orgId: orgId.ToString(), orgIds: orgIds, termsAccepted: termsAccepted));
        client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);

        return client;
    }

    private string GenerateB2CDummyToken(
        string scope = "",
        string sub = "03bad0af-caeb-46e8-809c-1d35a5863bc7",
        string tin = "11223344",
        string cpn = "Producent A/S",
        string name = "Peter Producent",
        string issuer = "demo.energioprindelse.dk",
        string audience = "Users",
        string orgId = "03bad0af-caeb-46e8-809c-1d35a5863bc7",
        string orgIds = "",
        bool termsAccepted = true)
    {
        var claims = new Dictionary<string, object>()
        {
            { UserClaimName.Scope, scope },
            { JwtRegisteredClaimNames.Name, name },
            { ClaimType.OrgId, orgId },
            { ClaimType.OrgIds, orgIds },
            { ClaimType.OrgCvr, tin },
            { ClaimType.OrgName, cpn },
            { ClaimType.SubType, "User" },
            { ClaimType.TermsAccepted, termsAccepted.ToString() },
            { UserClaimName.AccessToken, "" },
            { UserClaimName.IdentityToken, "" },
            { UserClaimName.ProviderKeys, "" },
        };

        var signedJwtToken = new TokenSigner(B2CDummyPrivateKey).Sign(
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
