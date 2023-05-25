using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Electricity.Client;

namespace RegistryConnector.Worker.EventHandlers
{
    public static class Startup
    {
        public static void RegisterApplication(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(RegistryOptions.Registry);
            services.Configure<RegistryOptions>(options);

            services.AddSingleton<ProjectOriginRegistryEventHandler>();

            services.AddSingleton(x => new RegisterClient(options.GetValue<string>("Url")!));
        }

        public static void RegisterPoEventHandlers(this WebApplication app)
        {
            var poClient = app.Services.GetService<RegisterClient>();

            poClient!.Events += async (e) => await app.Services.GetService<ProjectOriginRegistryEventHandler>()!.OnRegistryEvents(e);
        }
    }
}
