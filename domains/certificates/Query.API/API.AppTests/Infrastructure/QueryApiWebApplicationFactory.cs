using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.AppTests.Mocks;
using API.DataSyncSyncer;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace API.AppTests.Infrastructure;

public class QueryApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public string MartenConnectionString { get; set; } = "";
    public string DataSyncUrl { get; set; } = "";
    public string RabbitMqUsername { get; set; } = "";
    public string RabbitMqPassword { get; set; } = "";
    public string RabbitMqUrl { get; set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Marten", MartenConnectionString);
        builder.UseSetting("Datasync:Url", DataSyncUrl);
        builder.UseSetting("IntegrationEventBus:Password", RabbitMqPassword);
        builder.UseSetting("IntegrationEventBus:Username", RabbitMqUsername);
        builder.UseSetting("IntegrationEventBus:Url", RabbitMqUrl);

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

    public IBus GetMassTransitBus() => Services.GetRequiredService<IBus>();

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

    public async Task AddContract(string subject, string gsrn, DateTimeOffset startDate, DataSyncWireMock dataSyncWireMock)
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: gsrn);

        using var client = CreateAuthenticatedClient(subject);
        var body = new { gsrn, startDate = startDate.ToUnixTimeSeconds() };
        var response = await client.PostAsJsonAsync("api/certificates/contracts", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
