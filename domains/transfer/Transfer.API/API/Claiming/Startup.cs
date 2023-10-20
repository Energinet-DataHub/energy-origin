using API.Claiming.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace API.Claiming
{
    public static class Startup
    {
        public static void AddClaimServices(this IServiceCollection services)
        {
            services.AddHostedService<ClaimWorker>();

            services.AddScoped<IClaimService, ClaimService>();
        }
    }
}
