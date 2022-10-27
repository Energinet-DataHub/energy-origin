using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Issuer.Worker.RegistryConnector.Health;

public class HealthCheckWorker : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        var currentMinute = DateTime.Now.Minute;
        if (currentMinute % 2 == 0)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy());
        }
        return Task.FromResult(
            HealthCheckResult.Unhealthy());
    }
}