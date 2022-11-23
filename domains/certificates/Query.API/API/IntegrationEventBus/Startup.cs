using API.GranularCertificateIssuer;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationEventBus;

public static class Startup
{
    public static void AddIntegrationEventBus(this IServiceCollection services) =>
        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddConsumer<EnergyMeasuredConsumer>(cc => cc.UseConcurrentMessageLimit(1));
            
            o.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });
}
