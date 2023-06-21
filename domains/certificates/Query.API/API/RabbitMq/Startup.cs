using System;
using API.GranularCertificateIssuer;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.RabbitMq;

public static class Startup
{
    public static void AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.RabbitMq));

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConsumer<EnergyMeasuredEventHandler>();
            o.AddConsumer<CertificateIssuedInRegistryEventHandler>();
            o.AddConsumer<CertificateRejectedInRegistryEventHandler>();

            o.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var url = $"rabbitmq://{options.Host}:{options.Port}";

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
