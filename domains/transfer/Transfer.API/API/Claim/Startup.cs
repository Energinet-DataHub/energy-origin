using API.Claim.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace API.Claim
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
