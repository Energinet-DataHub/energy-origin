using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using API.MeteringPoints.Api;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;

namespace Tests.MeteringPoints;
public class CustomMeterPointWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public string ConnectionString { get; set; } = "";

    public byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

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

        var mockClient = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
        builder.UseSetting("TokenValidation:Audience", "Users");
        builder.ConfigureTestServices(x => x.AddSingleton(mockClient));
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
    public void Start() => Server.Should().NotBeNull();
    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("EO_API_VERSION", ApiVersions.Version20240110);
        return client;
    }
    public HttpClient CreateAuthenticatedClient(string sub, string tin = "11223344", string name = "Peter Producent",
            string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f", string apiVersion = ApiVersions.Version20240110)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub, tin: tin, name: name, actor: actor));
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
}
