using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnergyOrigin.Setup.Health;

public static class HealthServiceCollectionExtensions
{

    public static IHealthChecksBuilder AddDefaultHealthChecks(this IServiceCollection services)
    {
        return services.AddHealthChecks()
            .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")!);
    }

    public static IEndpointRouteBuilder MapDefaultHealthChecks(this IEndpointRouteBuilder builder)
    {
        // Liveness
        builder.MapHealthChecks("/health", new HealthCheckOptions());

        // Startup
        builder.MapHealthChecks("/health/startup", new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains("ready")
        });

        // Readiness
        builder.MapHealthChecks("/health/ready", new HealthCheckOptions()
        {
            ResultStatusCodes = new Dictionary<HealthStatus, int>
            {
                { HealthStatus.Healthy, StatusCodes.Status200OK },
                { HealthStatus.Degraded, StatusCodes.Status503ServiceUnavailable },
                { HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable },
            }
        });

        return builder;
    }
}
