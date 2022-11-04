using System.Collections.Generic;
using System.Threading.Channels;
using API.DataSyncSyncer.Service;
using API.DataSyncSyncer.Service.IntegrationService;
using API.MasterDataService;
using Microsoft.Extensions.DependencyInjection;

namespace API.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddSingleton(Channel.CreateUnbounded<List<MasterData>>());
        services.AddTransient(svc => svc.GetRequiredService<Channel<List<MasterData>>>().Reader);
        services.AddTransient(svc => svc.GetRequiredService<Channel<List<MasterData>>>().Writer);

        services.AddTransient<IAwesomeQueue, AwesomeQueue>();
        services.AddTransient<IDataSyncProvider, DataSyncProvider>();

        services.AddHostedService<DataSyncSyncerWorker>();
    }
}
