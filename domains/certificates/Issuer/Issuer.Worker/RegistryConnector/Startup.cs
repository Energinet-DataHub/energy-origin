using Issuer.Worker.DataSyncSyncer;

namespace Issuer.Worker.RegistryConnector;

public static class Startup
{
    public static void AddRegistryConnector(this IServiceCollection services)
    {
        services.AddHostedService<RegistryConnectorWorker>();
    }
}
