using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using Transfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Service;

namespace TransferAgreementAutomation.Worker;

public class TransferAgreementsAutomationWorker(
    ILogger<TransferAgreementsAutomationWorker> logger,
    ITransferAgreementAutomationMetrics metrics,
    AutomationCache cache,
    IServiceProvider provider,
    IDbContextFactory<ApplicationDbContext> contextFactory)
    : BackgroundService
{
    private readonly MemoryCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementsAutomationWorker running at: {time}", DateTimeOffset.Now);
            metrics.ResetCertificatesTransferred();
            metrics.ResetTransferErrors();
            cache.Cache.Set(HealthEntries.Key, HealthEntries.Healthy, cacheOptions);

            using var scope = provider.CreateScope();
            var projectOriginWalletService = scope.ServiceProvider.GetRequiredService<IProjectOriginWalletService>();

            try
            {
                var transferAgreements = await GetAllTransferAgreements(stoppingToken);
                metrics.SetNumberOfTransferAgreements(transferAgreements.Count);

                foreach (var transferAgreement in transferAgreements)
                {
                    await projectOriginWalletService.TransferCertificates(transferAgreement);
                }
            }
            catch (Exception ex)
            {
                cache.Cache.Set(HealthEntries.Key, HealthEntries.Unhealthy, cacheOptions);
                logger.LogError("Something went wrong with the TransferAgreementsAutomationWorker: {exception}", ex);
            }

            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task<List<TransferAgreement>> GetAllTransferAgreements(CancellationToken stoppingToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(stoppingToken);

        return await dbContext.TransferAgreements.ToListAsync(stoppingToken);
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Sleeping until next full hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
    }
}
