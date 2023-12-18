using System;
using System.Text;
using Contracts;
using DataContext;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys;

namespace RegistryConnector.Worker.UnitTests.Factories;

public class RegistryConnectorApplicationFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; set; } = "";
    public RabbitMqOptions? RabbitMqOptions { get; set; }
    public ProjectOriginOptions? ProjectOriginOptions { get; set; } = new()
    {
        RegistryName = "foo",
        RegistryUrl = "http://exampleRegistry.com",
        WalletUrl = "http://exampleWalletUrl.com",
        Dk1IssuerPrivateKeyPem = Encoding.UTF8.GetBytes(Algorithms.Ed25519.GenerateNewPrivateKey().ExportPkixText()),
        Dk2IssuerPrivateKeyPem = Encoding.UTF8.GetBytes(Algorithms.Ed25519.GenerateNewPrivateKey().ExportPkixText())
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("Retry:DefaultFirstLevelRetryCount", "5");
        builder.UseSetting("Retry:DefaultSecondLevelRetryCount", "4");
        builder.UseSetting("Retry:RegistryTransactionStillProcessingRetryCount", "100");

        if (ProjectOriginOptions != null)
        {
            builder.UseSetting("ProjectOrigin:WalletUrl", ProjectOriginOptions.WalletUrl);
            builder.UseSetting("ProjectOrigin:RegistryName", ProjectOriginOptions.RegistryName);
            builder.UseSetting("ProjectOrigin:RegistryUrl", ProjectOriginOptions.RegistryUrl);
            builder.UseSetting("ProjectOrigin:Dk1IssuerPrivateKeyPem", Convert.ToBase64String(ProjectOriginOptions.Dk1IssuerPrivateKeyPem));
            builder.UseSetting("ProjectOrigin:Dk2IssuerPrivateKeyPem", Convert.ToBase64String(ProjectOriginOptions.Dk2IssuerPrivateKeyPem));
        }

        builder.ConfigureTestServices(services =>
        {
            if (RabbitMqOptions != null)
                services.AddSingleton(Options.Create(RabbitMqOptions));

            services.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(5);
                // Ensure masstransit bus is started when we run our health checks
                options.WaitUntilStarted = RabbitMqOptions != null;
            });
        });
    }

    public IServiceScope ServiceScope() => Services.CreateScope();

    public ApplicationDbContext GetDbContext() => Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext();

    public IPublishEndpoint GetMassTransitBus() => Services.GetRequiredService<IPublishEndpoint>();

    // Accessing the Server property ensures that the server is running
    public void Start() => Server.Should().NotBeNull();
}
