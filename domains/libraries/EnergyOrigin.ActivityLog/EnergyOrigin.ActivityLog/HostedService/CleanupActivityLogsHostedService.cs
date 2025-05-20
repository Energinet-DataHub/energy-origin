using EnergyOrigin.ActivityLog;
using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class CleanupActivityLogsHostedService<TContext> : BackgroundService
    where TContext : DbContext
{
    private readonly ILogger<CleanupActivityLogsHostedService<TContext>> logger;
    private readonly IServiceProvider services;
    private readonly IOptions<ActivityLogOptions> activityLogOptions;

    public CleanupActivityLogsHostedService(
        ILogger<CleanupActivityLogsHostedService<TContext>> logger,
        IServiceProvider services,
        IOptions<ActivityLogOptions> activityLogOptions)
    {
        this.logger = logger;
        this.services = services;
        this.activityLogOptions = activityLogOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(CleanupActivityLogsHostedService<TContext>)} running");

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(activityLogOptions.Value.CleanupIntervalInSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                if (await dbContext.Database.GetPendingMigrationsAsync(stoppingToken) is { } migrations && migrations.Any())
                {
                    logger.LogInformation("Waiting for EF migrations");
                    continue;
                }

                var deleted = await dbContext.Set<ActivityLogEntry>()
                    .Where(x => x.Timestamp < DateTimeOffset.UtcNow.AddDays(-1 * activityLogOptions.Value.CleanupActivityLogsOlderThanInDays))
                    .ExecuteDeleteAsync(stoppingToken);

                logger.LogInformation($"Cleaned up {deleted} activity log entries");
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation($"{nameof(CleanupActivityLogsHostedService<TContext>)} is stopping");
        }
    }
}
