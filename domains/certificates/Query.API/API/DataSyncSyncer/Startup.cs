using System;
using API.Configurations;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Persistence;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatasyncOptions>(configuration.GetSection(DatasyncOptions.Datasync));

        services.AddSingleton<IDataSyncClientFactory, DataSyncClientFactory>();
        services.AddHttpClient<IDataSyncClient, DataSyncClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });

        services.AddSingleton<DataSyncService>();
        services.AddSingleton<ISyncState, SyncState>();

        services.ConfigureMarten(o =>
        {
        });

        services.AddHostedService<DataSyncSyncerWorker>();
    }
}
