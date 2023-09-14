using System;
using API.Configurations;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Migration;
using API.DataSyncSyncer.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services)
    {
        services.AddDatasyncOptions();

        services.AddSingleton<IDataSyncClientFactory, DataSyncClientFactory>();
        services.AddHttpClient<IDataSyncClient, DataSyncClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });

        services.AddSingleton<DataSyncService>();
        services.AddSingleton<ISyncState, SyncState>();

        services.AddHostedService<DataSyncSyncerWorker>();

        //TODO: Remove after migration
        services.AddSingleton<MartenHelper>();
        services.AddSingleton<DbContextHelper>();
        services.AddSingleton<MartenMigration>();
    }
}
