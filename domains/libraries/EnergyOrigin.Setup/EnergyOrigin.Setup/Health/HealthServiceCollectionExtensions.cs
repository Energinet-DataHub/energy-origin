using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        builder.MapHealthChecks("/health", new HealthCheckOptions());
        return builder;
    }
}
