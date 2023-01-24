using System.Reflection;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RabbitMQ;

public static class Startup
{
    public static void AddRabbitMq(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            // By default, sagas are in-memory, but should be changed to a durable
            // saga repository.
            x.SetInMemorySagaRepositoryProvider();

            var entryAssembly = Assembly.GetEntryAssembly();

            x.AddConsumers(entryAssembly);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("https://eo-u-rabbitmq.westeurope.cloudapp.azure.com", "/");
                cfg.ConfigureEndpoints(context);
            });
        });
        services.AddHostedService<Worker>();
    }
}
