using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.TransferAgreementCleanup.Options;
using DataContext;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.Transfer.TransferAgreementCleanup;

public class TransferAgreementCleanupWorker(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ILogger<TransferAgreementCleanupWorker> logger,
    IOptions<TransferAgreementCleanupOptions> options)
    : BackgroundService
{
    private readonly TransferAgreementCleanupOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementCleanupWorker running at: {Time}", DateTimeOffset.UtcNow);

            try
            {
                await using var dbContext = await contextFactory.CreateDbContextAsync(stoppingToken);
                if (await IsWaitingForMigrations(dbContext, stoppingToken))
                {
                    logger.LogInformation("Waiting for EF migrations");
                }
                else
                {
                    logger.LogInformation("Waiting for EF migrations");
                }
            }
            catch (Exception e)
            {
                logger.LogError("Something went wrong with the TransferAgreementCleanupWorker: {Exception}", e);
            }

            await Sleep(stoppingToken);
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

    private async Task<bool> IsWaitingForMigrations(DbContext dbContext, CancellationToken cancellationToken)
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        return pendingMigrations is null || pendingMigrations.Any();
    }
}
