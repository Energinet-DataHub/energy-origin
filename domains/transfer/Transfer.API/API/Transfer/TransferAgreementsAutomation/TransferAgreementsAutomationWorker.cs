using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Shared.Data;
using API.Transfer.Api.Models;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementsAutomation.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Transfer.TransferAgreementsAutomation;

public class TransferAgreementsAutomationWorker : BackgroundService
{
    private readonly ILogger<TransferAgreementsAutomationWorker> logger;
    private readonly ITransferAgreementAutomationMetrics metrics;
    private readonly AutomationCache memoryCache;
    private readonly IProjectOriginWalletService projectOriginWalletService;
    private readonly IDbContextFactory<ApplicationDbContext> contextFactory;

    private readonly MemoryCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
    };

    public TransferAgreementsAutomationWorker(
        ILogger<TransferAgreementsAutomationWorker> logger,
        ITransferAgreementAutomationMetrics metrics,
        AutomationCache memoryCache,
        IProjectOriginWalletService projectOriginWalletService,
        IDbContextFactory<ApplicationDbContext> contextFactory
    )
    {
        this.logger = logger;
        this.metrics = metrics;
        this.memoryCache = memoryCache;
        this.projectOriginWalletService = projectOriginWalletService;
        this.contextFactory = contextFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementsAutomationWorker running at: {time}", DateTimeOffset.Now);
            metrics.ResetCertificatesTransferred();
            metrics.ResetTransferErrors();
            memoryCache.Cache.Set(HealthEntries.Key, HealthEntries.Healthy, cacheOptions);

            try
            {
                var tas = await GetAllTransferAgreements(stoppingToken);
                metrics.SetNumberOfTransferAgreements(tas.Count);

                foreach (var transferAgreement in tas)
                {
                    await projectOriginWalletService.TransferCertificates(transferAgreement);
                }
            }
            catch (Exception ex)
            {
                memoryCache.Cache.Set(HealthEntries.Key, HealthEntries.Unhealthy, cacheOptions);
                logger.LogWarning("Something went wrong with the TransferAgreementsAutomationWorker: {exception}", ex);
            }

            await SleepToNearestHour(stoppingToken);
        }
    }

    private async Task<List<TransferAgreement>> GetAllTransferAgreements(CancellationToken stoppingToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(stoppingToken);

        return await context.TransferAgreements.ToListAsync(stoppingToken);
    }

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Sleeping until next full hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
    }
}
