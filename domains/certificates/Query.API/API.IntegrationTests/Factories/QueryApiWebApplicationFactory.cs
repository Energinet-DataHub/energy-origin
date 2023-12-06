using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.Configurations;
using API.DataSyncSyncer;
using API.IntegrationTests.Mocks;
using Asp.Versioning.ApiExplorer;
using Contracts;
using DataContext;
using DataContext.ValueObjects;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.WalletSystem.V1;
using Claim = System.Security.Claims.Claim;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests.Factories;

public class QueryApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly List<GrpcChannel> disposableChannels = new();

    public string ConnectionString { get; set; } = "";
    public string DataSyncUrl { get; set; } = "foo";
    public string WalletUrl { get; set; } = "bar";
    public RabbitMqOptions? RabbitMqOptions { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("Datasync:Url", DataSyncUrl);
        builder.UseSetting("Wallet:Url", WalletUrl);
        builder.UseSetting("RabbitMq:Password", RabbitMqOptions?.Password ?? "");
        builder.UseSetting("RabbitMq:Username", RabbitMqOptions?.Username ?? "");
        builder.UseSetting("RabbitMq:Host", RabbitMqOptions?.Host ?? "localhost");
        builder.UseSetting("RabbitMq:Port", RabbitMqOptions?.Port.ToString() ?? "4242");

        builder.ConfigureTestServices(services =>
        {
            services.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(5);
                // Ensure masstransit bus is started when we run our health checks
                options.WaitUntilStarted = RabbitMqOptions != null;
            });

            // Remove DataSyncSyncerWorker
            services.Remove(services.First(s => s.ImplementationType == typeof(DataSyncSyncerWorker)));
        });
    }

    public IApiVersionDescriptionProvider GetApiVersionDescriptionProvider()
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        return provider;
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

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    public HttpClient CreateAuthenticatedClient(string subject, string apiVersion = "20230101")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(subject: subject));
        client.DefaultRequestHeaders.Add("EO_API_VERSION", apiVersion);

        return client;
    }

    public (WalletService.WalletServiceClient, Metadata metadata) CreateWalletClient(string subject)
    {
        var authentication = new AuthenticationHeaderValue("Bearer", GenerateToken(subject: subject));
        var metadata = new Metadata { { "Authorization", $"{authentication.Scheme} {authentication.Parameter}" } };

        var options = Services.GetRequiredService<IOptions<WalletOptions>>().Value;
        var channel = GrpcChannel.ForAddress(options.Url);
        disposableChannels.Add(channel);

        return (new WalletService.WalletServiceClient(channel), metadata);
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
            new Claim("sub", subject),
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

    public async Task AddContract(string subject,
        string gsrn,
        DateTimeOffset startDate,
        MeteringPointType meteringPointType,
        DataSyncWireMock dataSyncWireMock,
        Technology technology = null!)
    {
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn: gsrn, type: meteringPointType, technology: technology);

        using var client = CreateAuthenticatedClient(subject);
        var body = new { gsrn, startDate = startDate.ToUnixTimeSeconds() };
        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public void Start() => Server.Should().NotBeNull();

    protected override void Dispose(bool disposing)
    {
        foreach (var disposableChannel in disposableChannels)
        {
            disposableChannel.Dispose();
        }

        base.Dispose(disposing);
    }
}
