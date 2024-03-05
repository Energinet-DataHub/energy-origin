using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.IssuingContractCleanup;

public class IssuingContractCleanupWorker(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ILogger<IssuingContractCleanupWorker> logger,
    IOptions<IssuingContractCleanupOptions> options) : BackgroundService
{
    private readonly IssuingContractCleanupOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("IssuingContractCleanupWorker running at: {Time}", DateTimeOffset.UtcNow);

            try
            {
                await DeleteExpiredIssuingContracts(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError("Something went wrong with the IssuingContractCleanupWorker: {Exception}", e);
            }

            await Sleep(stoppingToken);
        }
    }

    private async Task DeleteExpiredIssuingContracts(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var expiredIssuingContracts = context.Contracts
            .Where(c => c.EndDate != null && c.EndDate < DateTimeOffset.UtcNow);

        context.Contracts.RemoveRange(expiredIssuingContracts);

        await context.SaveChangesAsync(cancellationToken);
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
