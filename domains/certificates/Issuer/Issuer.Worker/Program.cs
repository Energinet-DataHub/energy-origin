using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Issuer.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IEventStore, MemoryEventStore>();

        services.AddHostedService<Worker1>();
        services.AddHostedService<Worker2>();
        services.AddHostedService<Worker3>();
    })
    .Build();

await host.RunAsync();
