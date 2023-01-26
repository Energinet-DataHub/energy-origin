using API.RabbitMQ.Consumers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace API.RabbitMQ;

public static class Startup
{
    public static void AddRabbitMq(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            // By default, sagas are in-memory, but should be changed to a durable
            // saga repository.
            x.SetInMemorySagaRepositoryProvider();

            x.AddConsumer<PocConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("eo-rabbitmq-service", "/");
                cfg.ConfigureEndpoints(context);
            });
        });
        services.AddHostedService<Worker>();
    }
}
