using System;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Configurations;
using API.DataSyncSyncer.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services, IConfiguration configuration)
    {
        DatasyncOptions options = new();
        configuration.GetSection(DatasyncOptions.Datasync).Bind(options);

        services.AddHttpClient<IDataSyncClient, DataSyncClient>(client => client.BaseAddress = new Uri(options.Url));

        services.AddSingleton<DataSyncService>();
        services.AddTransient<IDataSyncClient, DataSyncClient>();
        services.AddSingleton<ISyncState, SyncState>();

        services.AddHostedService<DataSyncSyncerWorker>();
    }
}
