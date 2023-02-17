using API.DemoWorkflow;
using API.GranularCertificateIssuer;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationEventBus;

public static class Startup
{
    public static void AddIntegrationEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        var endpointConfiguration = new DemoSendEndpointConfiguration();
        services.AddSingleton(endpointConfiguration);

        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            o.AddSagaStateMachine<DemoStateMachine, DemoStateMachineInstance>()
                .MartenRepository(configuration.GetConnectionString("Marten"), r =>
                {
                    r.Schema.For<DemoStateMachineInstance>().UseOptimisticConcurrency(true);
                });

            o.AddConsumer<RegistryConnectorDemoConsumer>().Endpoint(c => { c.Name = endpointConfiguration.RegistryConnector; });
            o.AddConsumer<EnergyMeasuredConsumer>(cc => cc.UseConcurrentMessageLimit(1));

            o.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });
    }
}
