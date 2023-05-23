extern alias registryConnector;
using System;
using System.Net.Http;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using registryConnector::RegistryConnector.Worker;
using RabbitMqOptions = API.RabbitMq.Configurations.RabbitMqOptions;

namespace API.IntegrationTests.Factories;
extern alias registryConnector;
public class RegistryConnectorApplicationFactory : WebApplicationFactory<registryConnector::Program>
{
    public RabbitMqOptions? RabbitMqOptions { get; set; }
    public RegistryOptions? RegistryOptions { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RabbitMq:Password", RabbitMqOptions?.Password ?? "");
        builder.UseSetting("RabbitMq:Username", RabbitMqOptions?.Username ?? "");
        builder.UseSetting("RabbitMq:Host", RabbitMqOptions?.Host ?? "localhost");
        builder.UseSetting("RabbitMq:Port", RabbitMqOptions?.Port.ToString() ?? "4242");

        builder.UseSetting("Registry:Url", RegistryOptions?.Url ?? "");
        builder.UseSetting("Registry:IssuerPrivateKeyPem", RegistryOptions?.IssuerPrivateKeyPem != null ? Convert.ToBase64String(RegistryOptions.IssuerPrivateKeyPem) : "");

        builder.ConfigureTestServices(services =>
        {
            services.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(5);
                // Ensure masstransit bus is started when we run our health checks
                options.WaitUntilStarted = RabbitMqOptions != null;
            });
        });
    }

    public new HttpClient StartWebApp() => CreateClient();
}
