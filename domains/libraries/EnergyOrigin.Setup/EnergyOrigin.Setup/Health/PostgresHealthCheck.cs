using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace EnergyOrigin.Setup.Health;

public class DbContextHealthCheck<TContext>(IDbContextFactory<TContext> factory) : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = factory.CreateDbContext();
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cannot connect to database");
        }
        catch (NpgsqlException ex) when (IsTransient(ex))
        {
            return HealthCheckResult.Degraded("Transient DB connectivity issue");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Unexpected DB issue", ex);
        }
    }

    private static bool IsTransient(NpgsqlException ex) =>
        ex.SqlState is "57P01" or "08006" or "53300";
}
