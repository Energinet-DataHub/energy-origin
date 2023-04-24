using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.Electricity.Client;

namespace API.Application.RegistryConnector
{
    public static class Startup
    {
        public static void RegisterPoClient(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(RegistryOptions.Registry);
            services.Configure<RegistryOptions>(options);

            services.AddSingleton<PoRegistryEventListener>();

            services.AddSingleton(x =>
            {
                var client = new RegisterClient(options.GetValue<string>("Url"));
                return client;
            });
        }
    }
}
