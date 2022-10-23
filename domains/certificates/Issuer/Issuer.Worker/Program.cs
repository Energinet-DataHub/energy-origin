using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Issuer.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IEventStore, MemoryEventStore>();

        services.AddHostedService<Worker1>();
        services.AddHostedService<Worker2>();
        services.AddHostedService<Worker3>();
    })
    .Build();

await host.RunAsync();

/*
 * TODOS:
 * - BackgroundServices that subscribe to events currently ignores the pointer
 * - Use Serilog for logging
 * - Add health checks and figure out how to configure k8s probes (http endpoint, file or ?)
 * - What topics to use
 * - IEventConsumerBuilder does not have async support, so what will do?
 * - Use the right events
 */
