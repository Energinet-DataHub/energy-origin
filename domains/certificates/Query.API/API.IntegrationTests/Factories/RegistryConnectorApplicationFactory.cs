extern alias registryConnector;
using Contracts;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys;
using registryConnector::RegistryConnector.Worker;
using System;
using System.Text;

namespace API.IntegrationTests.Factories;

public class RegistryConnectorApplicationFactory : WebApplicationFactory<registryConnector::Program>
{
    public string ConnectionString { get; set; } = "";
    public RabbitMqOptions? RabbitMqOptions { get; set; }
    public string OtlpReceiverEndpoint { get; private set; } = "http://foo";
    public ProjectOriginOptions? ProjectOriginOptions { get; set; } = new()
    {
        RegistryName = "foo",
        RegistryUrl = "bar",
        WalletUrl = "baz",
        Dk1IssuerPrivateKeyPem = Encoding.UTF8.GetBytes(Algorithms.Ed25519.GenerateNewPrivateKey().ExportPkixText()),
        Dk2IssuerPrivateKeyPem = Encoding.UTF8.GetBytes(Algorithms.Ed25519.GenerateNewPrivateKey().ExportPkixText())
    };

    public RetryOptions RetryOptions { get; set; } = new()
    {
        DefaultFirstLevelRetryCount = 5,
        DefaultSecondLevelRetryCount = 4,
        RegistryTransactionStillProcessingRetryCount = 100
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", ConnectionString);
        builder.UseSetting("Otlp:ReceiverEndpoint", OtlpReceiverEndpoint);
        builder.UseSetting("Retry:DefaultFirstLevelRetryCount", RetryOptions.DefaultFirstLevelRetryCount.ToString());
        builder.UseSetting("Retry:DefaultSecondLevelRetryCount", RetryOptions.DefaultSecondLevelRetryCount.ToString());
        builder.UseSetting("Retry:RegistryTransactionStillProcessingRetryCount", RetryOptions.RegistryTransactionStillProcessingRetryCount.ToString());

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

    // Accessing the Server property ensures that the server is running
    public void Start() => Server.Should().NotBeNull();
}
