using System;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.IntegrationTests.Factories;

public class TransferAgreementsApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer = new PostgreSqlBuilder().Build();

    public Task InitializeAsync() => testContainer.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => testContainer.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(s => s.Configure<DatabaseOptions>(o =>
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
        }));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        var serviceScope = host.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        return host;
    }

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    public HttpClient CreateAuthenticatedClient(string sub)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(sub: sub));

        return client;
    }

    private static string GenerateToken(
        string scope = "",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
        string sub = "bdcb3287-3dd3-44cd-8423-1f94437648cc")
    {
        var key = Encoding.ASCII.GetBytes("TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST");

        var claims = new[]
        {
            new Claim("sub", sub),
            new Claim("scope", scope),
            new Claim("actor", actor)
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
}
