using System;
using API.DataSyncSyncer.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services, IConfiguration configuration)
    {
        var datasyncUrl = configuration.GetSection(DatasyncOptions.Datasync).Value;
        services.AddHttpClient<IDataSync, DataSync>(client => client.BaseAddress = new Uri(datasyncUrl));

        services.AddTransient<IDataSync, DataSync>();
        services.AddHostedService<DataSyncSyncerWorker>();
    }
}
