extern alias registryConnector;
using System;
using Contracts;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using registryConnector::RegistryConnector.Worker;

namespace API.IntegrationTests.Factories;

public class RegistryConnectorApplicationFactory : WebApplicationFactory<registryConnector::Program>
{
    public RabbitMqOptions? RabbitMqOptions { get; set; }
    public ProjectOriginOptions? ProjectOriginOptions { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
