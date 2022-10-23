namespace Issuer.Worker.DataSyncSyncer;

public static class Startup
{
    public static void AddDataSyncSyncer(this IServiceCollection services)
    {
        services.AddHostedService<DataSyncSyncerWorker>();
    }
}
