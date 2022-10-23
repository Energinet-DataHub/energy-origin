using Issuer.Worker.DataSyncSyncer;
using Microsoft.Extensions.DependencyInjection;

namespace Issuer.Worker.RegistryConnector;

public static class Startup
{
    public static void AddRegistryConnector(this IServiceCollection services)
    {
        services.AddHostedService<RegistryConnectorWorker>();
    }
}
