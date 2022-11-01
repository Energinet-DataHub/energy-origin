using Microsoft.Extensions.DependencyInjection;

namespace API.RegistryConnector;

public static class Startup
{
    public static void AddRegistryConnector(this IServiceCollection services)
    {
        services.AddHostedService<RegistryConnectorWorker>();
    }
}
