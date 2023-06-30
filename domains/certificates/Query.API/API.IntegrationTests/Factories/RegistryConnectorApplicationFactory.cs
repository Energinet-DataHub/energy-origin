extern alias registryConnector;
using System;
using Contracts;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using registryConnector::RegistryConnector.Worker;

namespace API.IntegrationTests.Factories;

public class RegistryConnectorApplicationFactory : WebApplicationFactory<registryConnector::Program>
{
    public RabbitMqOptions? RabbitMqOptions { get; set; }
    public RegistryOptions? RegistryOptions { get; set; }
    public ProjectOriginOptions? ProjectOriginOptions { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RabbitMq:Password", RabbitMqOptions?.Password ?? "");
        builder.UseSetting("RabbitMq:Username", RabbitMqOptions?.Username ?? "");
        builder.UseSetting("RabbitMq:Host", RabbitMqOptions?.Host ?? "localhost");
        builder.UseSetting("RabbitMq:Port", RabbitMqOptions?.Port.ToString() ?? "4242");

        builder.UseSetting("Registry:Url", RegistryOptions?.Url ?? "http://localhost");
        builder.UseSetting("Registry:IssuerPrivateKeyPem", RegistryOptions?.IssuerPrivateKeyPem != null ? Convert.ToBase64String(RegistryOptions.IssuerPrivateKeyPem) : "");

        builder.ConfigureTestServices(services =>
        {
            if (ProjectOriginOptions != null)
            {
                services.Configure<ProjectOriginOptions>(options =>
                {
                    //TODO: Can this be prettier?
                    options.RegistryName = ProjectOriginOptions.RegistryName;
                    options.RegistryUrl = ProjectOriginOptions.RegistryUrl;
                    options.Dk1IssuerPrivateKeyPem = ProjectOriginOptions.Dk1IssuerPrivateKeyPem;
                    options.Dk2IssuerPrivateKeyPem = ProjectOriginOptions.Dk2IssuerPrivateKeyPem;
                    options.WalletUrl = ProjectOriginOptions.WalletUrl;
                });
            }

            //services.AddOptions<ProjectOriginOptions>().Bind()
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
