using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using API.Authorization.Controllers;
using API.Models;
using EnergyOrigin.TokenValidation.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace API.IntegrationTests.Setup;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal string ConnectionString { get; set; } = "";
    public readonly Guid IssuerIdpClientId = Guid.NewGuid();
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
        builder.UseSetting("B2C:CustomPolicyClientId", IssuerIdpClientId.ToString());
        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
        builder.UseSetting("TokenValidation:Audience", "Users");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveDbContext<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
            });

            services.EnsureDbCreated<ApplicationDbContext>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        ReplaceB2CAuthenticationSchemes(host);
        return host;
    }

    private static void ReplaceB2CAuthenticationSchemes(IHost host)
    {
        var authenticationSchemeProvider = host.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        authenticationSchemeProvider.RemoveScheme(EnergyOrigin.TokenValidation.b2c.AuthenticationScheme
            .B2CAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(EnergyOrigin.TokenValidation.b2c.AuthenticationScheme
            .B2CClientCredentialsCustomPolicyAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(EnergyOrigin.TokenValidation.b2c.AuthenticationScheme
            .B2CMitIDCustomPolicyAuthenticationScheme);

        var b2CScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CAuthenticationScheme,
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CAuthenticationScheme,
            typeof(TestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CScheme);

        var b2CMitIdScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme,
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme,
            typeof(TestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CMitIdScheme);

        var b2CClientCredentialsScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            EnergyOrigin.TokenValidation.b2c.AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            typeof(TestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CClientCredentialsScheme);
    }

    public Api CreateApi(string sub = "", string name = "", string orgIds = "", string subType = "", string orgCvr = "12345678", string orgName = "Test Org")
    {
        sub = string.IsNullOrEmpty(sub) ? Guid.NewGuid().ToString() : sub;
        name = string.IsNullOrEmpty(name) ? "Test Testesen" : name;
        orgIds = string.IsNullOrEmpty(orgIds) ? Guid.NewGuid().ToString() : orgIds;
        subType = string.IsNullOrEmpty(subType) ? "user" : subType;

        return new Api(CreateAuthenticatedClient(sub, name, orgIds, subType, orgCvr, orgName));
    }

    private HttpClient CreateAuthenticatedClient(string sub, string name, string orgIds, string subType, string orgCvr, string orgName)
    {
        var httpClient = CreateClient();
        var token = GenerateToken(sub, name, orgIds, subType, orgCvr, orgName);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Add("EO_API_VERSION", ApiVersions.Version20230101);
        return httpClient;
    }

    private string GenerateToken(string sub, string name, string orgIds, string subType, string orgCvr, string orgName)
    {
        using RSA rsa = RSA.Create(2048 * 2);
        var req = new CertificateRequest("cn=eotest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.DigitalSignature,
                true));
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        var signingCredentials = new SigningCredentials(new X509SecurityKey(cert), SecurityAlgorithms.RsaSha256);
        var tokenHandler = new JwtSecurityTokenHandler();
        var identity = new ClaimsIdentity(new List<Claim>
        {
            new("sub", sub),
            new("name", name),
            new("org_ids", orgIds),
            new("sub_type", subType),
            new("org_cvr", orgCvr),
            new("org_name", orgName)
        });
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = "audience",
            Issuer = "issuer",
            NotBefore = DateTime.Now.AddHours(-1),
            Expires = DateTime.Now.AddHours(1),
            SigningCredentials = signingCredentials,
            Subject = identity
        };

        var token = tokenHandler.CreateToken(securityTokenDescriptor);
        var encodedAccessToken = tokenHandler.WriteToken(token);
        return encodedAccessToken!;
    }

    public Task InitializeAsync()
    {
        Server.Should().NotBeNull();
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}

public static class ServiceCollectionExtensions
{
    public static void RemoveDbContext<T>(this IServiceCollection services) where T : DbContext
    {
        var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<T>));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

    public static void EnsureDbCreated<T>(this IServiceCollection services) where T : DbContext
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var context = serviceProvider.GetRequiredService<T>();
        context.Database.EnsureCreated();
    }
}
