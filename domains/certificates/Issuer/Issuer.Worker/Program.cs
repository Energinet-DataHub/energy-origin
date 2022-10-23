using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Issuer.Worker.DataSyncSyncer;
using Issuer.Worker.GranularCertificateIssuer;
using Issuer.Worker.QueryModelUpdater;
using Issuer.Worker.RegistryConnector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((h, builder) =>
    {
        builder.ClearProviders();

        var loggerConfiguration = new LoggerConfiguration();

        loggerConfiguration = h.HostingEnvironment.IsEnvironment("development")
            ? loggerConfiguration.WriteTo.Console()
            : loggerConfiguration.WriteTo.Console(new JsonFormatter());
        
        builder.AddSerilog(loggerConfiguration.CreateLogger());
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IEventStore, MemoryEventStore>();
        
        services.AddDataSyncSyncer();
        services.AddGranularCertificateIssuer();
        services.AddRegistryConnector();
        services.AddQueryModelUpdater();
    })
    .Build();

await host.RunAsync();

/*
 * TODOS:
 * - BackgroundServices that subscribe to events currently ignores the pointer
 * - Add health checks and figure out how to configure k8s probes (http endpoint, file or ?) - maybe this is for different task?
 * - What topics to use
 * - IEventConsumerBuilder does not have async support, so what will we do?
 * - Use the right events
 * - Switch to using Log.LogDebug statements
 * - Add Unit test project
 */
