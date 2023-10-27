using API.Claiming.Api.Repositories;
using API.Claiming.Automation;
using API.Claiming.Automation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace API.Claiming;

public static class Startup
{
    public static void AddClaimServices(this IServiceCollection services)
    {
        services.AddScoped<IClaimRepository, ClaimRepository>();

        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<IProjectOriginWalletService, ProjectOriginWalletService>();

        services.AddHostedService<ClaimWorker>();
    }
}
