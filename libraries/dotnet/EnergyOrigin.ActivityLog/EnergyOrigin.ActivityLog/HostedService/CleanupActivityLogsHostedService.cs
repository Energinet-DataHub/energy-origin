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
                await DeleteActivityLogs();
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation($"{nameof(CleanupActivityLogsHostedService)} is stopping");
        }
    }

    private async Task DeleteActivityLogs()
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var deleted = await dbContext.Set<ActivityLogEntry>()
            .Where(x => x.Timestamp < DateTimeOffset.UtcNow.AddDays(-1 * activityLogOptions.Value.CleanupActivityLogsOlderThanInDays))
            .ExecuteDeleteAsync();

        logger.LogInformation($"{nameof(CleanupActivityLogsHostedService)} cleaned up: {deleted} activity log entries",
            deleted);
    }
}
