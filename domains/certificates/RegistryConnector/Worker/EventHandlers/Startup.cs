using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.Client;

namespace RegistryConnector.Worker.EventHandlers;

public static class Startup
{
    public static void RegisterEventHandlers(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RegistryOptions>(configuration.GetSection(RegistryOptions.Registry));

        services.AddSingleton<IssuerKey>();
        services.AddSingleton<ProjectOriginRegistryEventHandler>();

        services.AddSingleton(sp =>
        {
            var url = sp.GetRequiredService<IOptions<RegistryOptions>>().Value.Url;
            return new RegisterClient(url);
        });
    }

    public static void SetupRegistryEvents(this WebApplication app)
    {
        var poClient = app.Services.GetService<RegisterClient>();

        poClient!.Events += async (e) => await app.Services.GetService<ProjectOriginRegistryEventHandler>()!.OnRegistryEvents(e);
    }
}
