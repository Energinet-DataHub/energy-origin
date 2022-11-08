using System.Threading.Channels;
using API.DataSyncSyncer.Service.Configurations;
using API.DataSyncSyncer.Service.Datasync;
using API.DataSyncSyncer.Service.Integration;
using CertificateEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

        services.Configure<DatasyncOptions>(
            configuration.GetSection(DatasyncOptions.Datasync)
        );

        services.AddSingleton(Channel.CreateUnbounded<EnergyMeasuredIntegrationEvent>());
        services.AddTransient(svc => svc.GetRequiredService<Channel<EnergyMeasuredIntegrationEvent>>().Writer);

        services.AddSingleton<IIntegrationEventBus, IntegrationEventBus>();
        services.AddTransient<IDataSync, DataSync>();
        services.AddHostedService<DataSyncSyncerWorker>();
    }
}
