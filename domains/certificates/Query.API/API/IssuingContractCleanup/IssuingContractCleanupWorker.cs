using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.IssuingContractCleanup;

public class IssuingContractCleanupWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<IssuingContractCleanupWorker> logger,
    IOptions<IssuingContractCleanupOptions> options) : BackgroundService
{
    private readonly IssuingContractCleanupOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("IssuingContractCleanupWorker running at: {Time}", DateTimeOffset.UtcNow);
            await PerformPeriodicTask(stoppingToken);
            await Sleep(stoppingToken);
        }
    }

    private async Task PerformPeriodicTask(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scopedCleanupService = scope.ServiceProvider.GetService<IssuingContractCleanupService>()!;
            await scopedCleanupService.RunCleanupJob(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError("Something went wrong with the IssuingContractCleanupWorker: {Exception}", e);
        }

    }

    private async Task Sleep(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sleep for {SleepTime}", options.SleepTime);
        try
        {
            await Task.Delay(options.SleepTime, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("Sleep was cancelled");
        }
    }
}
