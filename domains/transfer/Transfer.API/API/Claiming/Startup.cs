using API.Claiming.Api.Repositories;
using API.Claiming.Automation;
using API.Claiming.Automation.Services;
using API.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace API.Claiming;

public static class Startup
{
    public static void AddClaimServices(this IServiceCollection services)
    {
        services.AddScoped<IClaimAutomationRepository, ClaimAutomationRepository>();

        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<IProjectOriginWalletService, ProjectOriginWalletService>();
        services.AddScoped<IShuffler, Shuffler>();

        services.AddHostedService<ClaimWorker>();
    }
}
