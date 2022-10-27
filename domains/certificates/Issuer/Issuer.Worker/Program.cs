using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using Issuer.Worker.DataSyncSyncer;
using Issuer.Worker.GranularCertificateIssuer;
using Issuer.Worker.MasterDataService;
using Issuer.Worker.QueryModelUpdater;
using Issuer.Worker.RegistryConnector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((context, builder) =>
    {
        builder.ClearProviders();

        var loggerConfiguration = new LoggerConfiguration();

        loggerConfiguration = context.HostingEnvironment.IsDevelopment()
            ? loggerConfiguration.WriteTo.Console()
            : loggerConfiguration.WriteTo.Console(new JsonFormatter());

        builder.AddSerilog(loggerConfiguration.CreateLogger());
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IEventStore, MemoryEventStore>();

        services.AddMasterDataService(context.Configuration);
        services.AddDataSyncSyncer();
        services.AddGranularCertificateIssuer();
        services.AddRegistryConnector();
        services.AddQueryModelUpdater();
    })
    .Build();

await host.RunAsync();
