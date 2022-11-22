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
        var datasyncUrl = configuration.GetSection(DatasyncOptions.Datasync).Value;
        services.AddHttpClient<DataSyncService>(client => client.BaseAddress = new Uri(datasyncUrl));

        services.AddTransient<DataSyncService>();
        services.AddTransient<IDataSyncClient, DataSyncClient>();
        services.AddTransient<IState, State>();

        services.AddHostedService<DataSyncSyncerWorker>();
    }
}
