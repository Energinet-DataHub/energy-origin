using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.DataSyncSyncer;
using API.Query.API.ApiModels;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace API.AppTests;

public class MartenFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer testContainer;

    public MartenFixture()
    {
        testContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            //.WithCleanUp(true)
            .WithDatabase(new PostgreSqlTestcontainerConfiguration
            {
                Database = "marten",
                Username = "postgres",
                Password = "postgres",
            })
            .WithImage("sibedge/postgres-plv8")
            .Build();
    }

    public string ConnectionString => testContainer.ConnectionString;

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();

        var result = await testContainer.ExecAsync(new[]
        {
            "/bin/sh", "-c",
            "psql -U postgres -c \"CREATE EXTENSION plv8; SELECT extversion FROM pg_extension WHERE extname = 'plv8';\""
        });
    }

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}

public class DbTest : IClassFixture<MyApplicationFactory>
{
    private readonly MyApplicationFactory factory;

    public DbTest(MyApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task NoData()
    {
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetAsync("certificates");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task HasData()
    {
        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var createCertRes = await client.GetAsync("createcert");

        Assert.Equal(HttpStatusCode.OK, createCertRes.StatusCode);

        var response2 = await client.GetAsync("certificates");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var certificateList = await response2.Content.ReadFromJsonAsync<CertificateList>();
        Assert.Equal(1, certificateList?.Result.Count());
    }
}

public class MyApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MartenFixture martenFixture;

    public MyApplicationFactory()
    {
        martenFixture = new MartenFixture();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Marten", martenFixture.ConnectionString);

        builder.ConfigureTestServices(services =>
        {
            //Remove DataSyncSyncerWorker
            services.Remove(services.First(s => s.ImplementationType == typeof(DataSyncSyncerWorker)));
        });
    }

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    public HttpClient CreateAuthenticatedClient(string subject)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(subject: subject));

        return client;
    }

    private static string GenerateToken(
        string scope = "",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
        string subject = "bdcb3287-3dd3-44cd-8423-1f94437648cc")
    {
        var key = Encoding.ASCII.GetBytes("TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST");

        var claims = new[]
        {
            new Claim("subject", subject),
            new Claim("scope", scope),
            new Claim("actor", actor)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public Task InitializeAsync() => martenFixture.InitializeAsync();

    Task IAsyncLifetime.DisposeAsync() => martenFixture.DisposeAsync();
}
