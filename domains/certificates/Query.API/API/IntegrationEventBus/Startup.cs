using API.DemoWorkflow;
using API.GranularCertificateIssuer;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationEventBus;

public static class Startup
{
    public static void AddIntegrationEventBus(this IServiceCollection services)
    {
        var connectionString = "host=localhost;Port=5432;Database=marten;username=postgres;password=postgres;";

        var configuration = new DemoSendEndpointConfiguration();
        services.AddSingleton(configuration);

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddSagaStateMachine<DemoStateMachine, DemoStateMachineInstance>()
                .MartenRepository(connectionString, r =>
                {
                    r.Schema.For<DemoStateMachineInstance>().UseOptimisticConcurrency(true);
                });

            o.AddConsumer<RegistryConnectorDemoConsumer>().Endpoint(c => { c.Name = configuration.RegistryConnector; });
            o.AddConsumer<EnergyMeasuredConsumer>(cc => cc.UseConcurrentMessageLimit(1));

            o.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });
    }
}
