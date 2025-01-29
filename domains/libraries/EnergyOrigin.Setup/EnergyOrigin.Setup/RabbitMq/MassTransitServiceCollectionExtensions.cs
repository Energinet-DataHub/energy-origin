using System;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.Setup.RabbitMq;

public static class MassTransitServiceCollectionExtensions
{
    public static void AddMassTransitAndRabbitMq<TDbContext>(this IServiceCollection services, Action<IBusRegistrationConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.RabbitMq)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();
            o.AddConfigureEndpointsCallback((_, cfg) =>
            {
                if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                {
                    rmq.SetQuorumQueue(3);
                }
            });
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

            o.AddEntityFrameworkOutbox<TDbContext>(outboxConfigurator =>
            {
                outboxConfigurator
                    .UsePostgres()
                    .UseBusOutbox();
            });

            o.AddConfigureEndpointsCallback((ctx, _, configurator) =>
            {
                configurator.UseEntityFrameworkOutbox<TDbContext>(ctx);
            });

            configure?.Invoke(o);
        });
    }
}
