using System;
using API.Configurations;
using API.GranularCertificateIssuer;
using API.IntegrationEventBus.Configurations;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.IntegrationEventBus;

public static class Startup
{
    public static void AddIntegrationEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationEventBusOptions>(
            configuration.GetSection(IntegrationEventBusOptions.IntegrationEventBus));

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConsumer<EnergyMeasuredConsumer>(cc => cc.UseConcurrentMessageLimit(1));

            o.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<IntegrationEventBusOptions>>().Value;
                string url = $"rabbitmq://{options.Host}:{options.Port}";

                cfg.Host(new Uri(url), h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
