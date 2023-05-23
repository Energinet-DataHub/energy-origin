using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Electricity.Client;
using RegistryConnector.Worker.Application.PoEventHandlers;

namespace RegistryConnector.Worker.Application
{
    public static class Startup
    {
        public static void RegisterApplication(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(RegistryOptions.Registry);
            services.Configure<RegistryOptions>(options);

            services.AddSingleton<PoRegistryEventHandler>();

            services.AddSingleton(x => new RegisterClient(options.GetValue<string>("Url")!));
        }

        public static void RegisterPoEventHandlers(this WebApplication app)
        {
            var poClient = app.Services.GetService<RegisterClient>();

            if (poClient == null)
                throw new Exception("Project Origin registry client not registered.");

            poClient.Events += app.Services.GetService<PoRegistryEventHandler>()!.OnRegistryEvents;
        }
    }
}
