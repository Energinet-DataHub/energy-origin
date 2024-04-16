using Microsoft.Extensions.DependencyInjection;

namespace API.IssuingContractCleanup;

public static class Startup
{
    public static void AddIssuingContractCleanup(this IServiceCollection services)
    {
        services.AddOptions<IssuingContractCleanupOptions>()
            .BindConfiguration(IssuingContractCleanupOptions.Prefix)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IssuingContractCleanupService>();
        services.AddHostedService<IssuingContractCleanupWorker>();
    }
}
