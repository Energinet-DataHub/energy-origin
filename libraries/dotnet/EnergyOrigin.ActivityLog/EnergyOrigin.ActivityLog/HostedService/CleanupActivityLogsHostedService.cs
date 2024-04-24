using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.ActivityLog.HostedService;

public class CleanupActivityLogsHostedService(
    ILogger<CleanupActivityLogsHostedService> logger,
    IServiceProvider services, IOptions<ActivityLogOptions> activityLogOptions)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(CleanupActivityLogsHostedService)} running");

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(activityLogOptions.Value.CleanupIntervalInSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using (var scope = services.CreateScope())
                {
                    using var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
                    if (await IsWaitingForMigrations(dbContext, stoppingToken))
                    {
                        logger.LogInformation("Waiting for EF migrations");
                    }
                    else
                    {
                        await DeleteActivityLogs(dbContext);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation($"{nameof(CleanupActivityLogsHostedService)} is stopping");
        }
    }

    private async Task DeleteActivityLogs(DbContext dbContext)
    {
        var deleted = await dbContext.Set<ActivityLogEntry>()
            .Where(x => x.Timestamp < DateTimeOffset.UtcNow.AddDays(-1 * activityLogOptions.Value.CleanupActivityLogsOlderThanInDays))
            .ExecuteDeleteAsync();

        logger.LogInformation($"{nameof(CleanupActivityLogsHostedService)} cleaned up: {deleted} activity log entries",
            deleted);
    }

    private async Task<bool> IsWaitingForMigrations(DbContext dbContext, CancellationToken cancellationToken)
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        return pendingMigrations is null || pendingMigrations.Any();
    }
}
