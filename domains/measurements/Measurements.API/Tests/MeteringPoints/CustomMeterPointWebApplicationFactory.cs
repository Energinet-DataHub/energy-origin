using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using API.MeteringPoints.Api;
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
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using AuthenticationScheme = EnergyOrigin.TokenValidation.b2c.AuthenticationScheme;

namespace Tests.MeteringPoints;
public class CustomMeterPointWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public string ConnectionString { get; set; } = "";

    public byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

    private byte[] B2CDummyPrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

    public Meteringpoint.V1.Meteringpoint.MeteringpointClient MeteringPointClientMock { get; private set; } = null!;

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

        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
        builder.UseSetting("TokenValidation:Audience", "Users");

        builder.UseSetting("B2C:B2CWellKnownUrl",
            "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0/.well-known/openid-configuration");
        builder.UseSetting("B2C:ClientCredentialsCustomPolicyWellKnownUrl",
            "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_CLIENTCREDENTIALS");
        builder.UseSetting("B2C:MitIDCustomPolicyWellKnownUrl",
            "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_MITID");
        builder.UseSetting("B2C:Audience", "f00b9b4d-3c59-4c40-b209-2ef87e509f54");
        builder.UseSetting("B2C:CustomPolicyClientId", "a701d13c-2570-46fa-9aa2-8d81f0d8d60b");

        MeteringPointClientMock = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

        builder.ConfigureTestServices(x =>
        {
            x.AddSingleton(MeteringPointClientMock);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return host;

        var factory = host.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        using var dbContext = factory.CreateDbContext();
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

    public void Start() => Server.Should().NotBeNull();

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version20240110);
        return client;
    }
    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
            string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = ApiVersions.Version20240110)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name, actor: actor));
        client.DefaultRequestHeaders.Add("X-API-Version", apiVersion);
        return client;
    }

    public HttpClient CreateB2CAuthenticatedClient(Guid sub, Guid orgId, string tin = "11223344", string name = "Peter Producent",
        string apiVersion = ApiVersions.Version20240515)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateB2CDummyToken(sub: sub.ToString(), tin: tin, name: name, orgId: orgId.ToString()));
        client.DefaultRequestHeaders.Add("X-API-Version", apiVersion);

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

    private string GenerateB2CDummyToken(
        string scope = "",
        string sub = "03bad0af-caeb-46e8-809c-1d35a5863bc7",
        string tin = "11223344",
        string cpn = "Producent A/S",
        string name = "Peter Producent",
        string issuer = "demo.energioprindelse.dk",
        string audience = "Users",
        string orgId = "03bad0af-caeb-46e8-809c-1d35a5863bc7")
    {
        var claims = new Dictionary<string, object>()
        {
            { UserClaimName.Scope, scope },
            { JwtRegisteredClaimNames.Name, name },
            { ClaimType.OrgIds, orgId },
            { ClaimType.OrgCvr, tin },
            { ClaimType.OrgName, cpn },
            { ClaimType.SubType, "User" },
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
}
