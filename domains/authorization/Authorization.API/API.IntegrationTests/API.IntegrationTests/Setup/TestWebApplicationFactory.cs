using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using API.Models;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Migrations;
using EnergyOrigin.Setup.RabbitMq;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using AuthenticationScheme = EnergyOrigin.TokenValidation.b2c.AuthenticationScheme;

namespace API.IntegrationTests.Setup;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal string ConnectionString { get; set; } = "";
    internal RabbitMqOptions RabbitMqOptions { get; set; } = new();
    public readonly Guid IssuerIdpClientId = Guid.NewGuid();
    public readonly string AdminPortalClientId = "d216b90b-3872-498a-bc18-4941a0f4398e";
    public string WalletUrl { get; set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("B2C:CustomPolicyClientId", IssuerIdpClientId.ToString());
        builder.UseSetting("B2C:AdminPortalClientId", AdminPortalClientId);
        builder.UseSetting("MitID:URI", "https://pp.netseidbroker.dk/op");
        builder.UseSetting("ProjectOrigin:WalletUrl", WalletUrl);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveDbContext<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => { options.UseNpgsql(ConnectionString); });

            new DbMigrator(ConnectionString, typeof(Program).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();

            services.Configure<RabbitMqOptions>(options =>
            {
                options.Host = RabbitMqOptions.Host;
                options.Port = RabbitMqOptions.Port;
                options.Username = RabbitMqOptions.Username;
                options.Password = RabbitMqOptions.Password;
            });
        });
    }

    public void SetRabbitMqOptions(RabbitMqOptions options)
    {
        RabbitMqOptions = options;
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
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme);

        var b2CScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CAuthenticationScheme,
            AuthenticationScheme.B2CAuthenticationScheme,
            typeof(TestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CScheme);

        var b2CMitIdScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme,
            AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme,
            typeof(TestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CMitIdScheme);

        var b2CClientCredentialsScheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
            typeof(TestAuthHandler));
        authenticationSchemeProvider.AddScheme(b2CClientCredentialsScheme);

    }

    public Api CreateApi(string sub = "", string name = "", string orgId = "", string orgIds = "", string subType = "", string orgCvr = "12345678",
        string orgName = "Test Org", bool termsAccepted = true)
    {
        sub = string.IsNullOrEmpty(sub) ? Guid.NewGuid().ToString() : sub;
        name = string.IsNullOrEmpty(name) ? "Test Testesen" : name;
        orgId = string.IsNullOrEmpty(orgId) ? Guid.NewGuid().ToString() : orgId;
        orgIds = string.IsNullOrEmpty(orgIds) ? orgId : orgIds;
        subType = string.IsNullOrEmpty(subType) ? "user" : subType;

        return new Api(CreateAuthenticatedClient(sub, name, orgId, orgIds, subType, orgCvr, orgName, termsAccepted));
    }

    private HttpClient CreateAuthenticatedClient(string sub, string name, string orgId, string orgIds, string subType, string orgCvr, string orgName,
        bool termsAccepted)
    {
        var httpClient = CreateClient();
        var token = GenerateToken(sub, name, orgId, orgIds, subType, orgCvr, orgName, termsAccepted);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);
        return httpClient;
    }

    private string GenerateToken(string sub, string name, string orgId, string orgIds, string subType, string orgCvr, string orgName, bool termsAccepted)
    {
        using RSA rsa = RSA.Create(2048 * 2);
        var req = new CertificateRequest("cn=eotest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature,
                true));
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        var signingCredentials = new SigningCredentials(new X509SecurityKey(cert), SecurityAlgorithms.RsaSha256);
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new("sub", sub),
            new("name", name),
            new("org_id", orgId),
            new("org_ids", orgIds),
            new("sub_type", subType),
            new("org_cvr", orgCvr),
            new("org_name", orgName)
        };
        if (termsAccepted)
        {
            claims.Add(new("tos_accepted", true.ToString()));
        }
        var identity = new ClaimsIdentity(claims);
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
}
