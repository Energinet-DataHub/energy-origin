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
        logger.LogInformation($"{nameof(CleanupActivityLogsHostedService)} running.");

        // On startup, delete all activity logs older than 6 months
        await DeleteActivityLogsOlderThan6Months();

        using PeriodicTimer timer = new(TimeSpan.FromMinutes(activityLogOptions.Value.CleanupIntervalInMinutes));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DeleteActivityLogsOlderThan6Months();
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation($"{nameof(CleanupActivityLogsHostedService)} is stopping.");
        }
    }

    private async Task DeleteActivityLogsOlderThan6Months()
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var deleted = await dbContext.Set<ActivityLogEntry>()
            .Where(x => x.Timestamp < DateTimeOffset.UtcNow.AddDays(-1*activityLogOptions.Value.CleanupActivityLogsOlderThanInDays))
            .ExecuteDeleteAsync();

        logger.LogInformation($"{nameof(CleanupActivityLogsHostedService)} cleaned up: {deleted} activity log entries", deleted);
    }
}
