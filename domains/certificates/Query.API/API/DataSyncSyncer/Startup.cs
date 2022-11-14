using Microsoft.Extensions.DependencyInjection;

namespace API.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services)
        => services.AddHostedService<DataSyncSyncerWorker>();
}
