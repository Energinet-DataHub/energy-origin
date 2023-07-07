using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RegistryConnector.Worker.EventHandlers;

public static class Startup
{
    public static void RegisterEventHandlers(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RegistryOptions>(configuration.GetSection(RegistryOptions.Registry));

        services.Configure<ProjectOriginOptions>(configuration.GetSection(ProjectOriginOptions.ProjectOrigin));
    }
}
