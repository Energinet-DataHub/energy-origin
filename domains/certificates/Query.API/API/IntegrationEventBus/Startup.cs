using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationEventBus;

public static class Startup
{
    public static void AddIntegrationEventBus(this IServiceCollection services)
    {
        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            var entryAssembly = Assembly.GetEntryAssembly();
            o.AddConsumers(entryAssembly);

            o.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });
    }
}
