using System.Collections.Generic;
using System.Threading.Channels;
using API.DataSyncSyncer.Service;
using API.DataSyncSyncer.Service.Datasync;
using API.DataSyncSyncer.Service.Integration;
using API.MasterDataService;
using CertificateEvents;
using Microsoft.Extensions.DependencyInjection;

namespace API.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddSingleton(Channel.CreateUnbounded<EnergyMeasuredIntegrationEvent>());
        services.AddTransient(svc => svc.GetRequiredService<Channel<EnergyMeasuredIntegrationEvent>>().Writer);

        services.AddTransient<IIntegrationEventBus, IntegrationEventBus>();
        services.AddTransient<IDataSync, DataSync>();
        services.AddHostedService<DataSyncSyncerWorker>();
    }
}
