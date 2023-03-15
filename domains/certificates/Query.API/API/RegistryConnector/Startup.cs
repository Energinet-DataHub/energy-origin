using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.RegistryConnector;

public static class Startup
{
    public static void AddRegistryConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RegistryOptions>(configuration.GetSection(RegistryOptions.Registry));

        services.AddHostedService<RegistryWorker>();
    }
}
