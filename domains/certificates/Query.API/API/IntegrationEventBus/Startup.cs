using API.DemoWorkflow;
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

            // TODO: Can this be defined elsewhere, like in a startup-class for DemoWorkflow?
            o.AddSagaStateMachine<DemoStateMachine, DemoStateMachineInstance>()
                .InMemoryRepository(); //TODO: Change this to Marten

            // TODO: Can this be defined elsewhere, like in a startup-class for DemoWorkflow?
            o.AddConsumer<RegistryConnectorDemoConsumer>();
            o.AddConsumer<EnergyMeasuredConsumer>(cc => cc.UseConcurrentMessageLimit(1));

            o.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });
}
