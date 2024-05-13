using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using API.Authorization.Controllers;
using API.Models;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace API.IntegrationTests.Setup;

[CollectionDefinition(CollectionName)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string CollectionName = nameof(IntegrationTestCollection);
}

public class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public PostgresContainer PostgresContainer { get; } = new();

    public async Task InitializeAsync()
    {
        await PostgresContainer.InitializeAsync();

        using var scope = Services.CreateScope();
        await using var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
    }

    public Api CreateApi(string sub = "", string name = "", string orgIds = "", string subType = "")
    {
        sub = string.IsNullOrEmpty(sub) ? Guid.NewGuid().ToString() : sub;
        name = string.IsNullOrEmpty(name) ? "Test Testesen" : name;
        orgIds = string.IsNullOrEmpty(orgIds) ? Guid.NewGuid().ToString() : orgIds;
        subType = string.IsNullOrEmpty(subType) ? "user" : subType;

        return new Api(CreateAuthenticatedClient(sub, name, orgIds, subType));
    }

    private HttpClient CreateAuthenticatedClient(string sub, string name, string orgIds, string subType)
    {
        var httpClient = CreateClient();
        var token = GenerateToken(sub, name, orgIds, subType);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Add("EO_API_VERSION", ApiVersions.Version20230101);
        return httpClient;
    }

    private string GenerateToken(string sub, string name, string orgIds, string subType)
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
        });
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = "audience",
            Issuer = "issuer",
            NotBefore = DateTime.Now.AddHours(1),
            Expires = DateTime.Now.AddHours(1),
            SigningCredentials = signingCredentials,
            Subject = identity
        };

        var token = tokenHandler.CreateToken(securityTokenDescriptor);
        var encodedAccessToken = tokenHandler.WriteToken(token);
        return encodedAccessToken!;
    }

    public new async Task DisposeAsync()
    {
        await PostgresContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveDbContext<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(PostgresContainer.ConnectionString);
            });

            services.EnsureDbCreated<ApplicationDbContext>();

            services.AddAuthentication("Development")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Development", options => { });
        });
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
