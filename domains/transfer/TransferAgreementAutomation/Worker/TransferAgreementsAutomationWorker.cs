using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Shared.Data;
using API.Transfer.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransferAgreementAutomation.Worker.Metrics;
using IProjectOriginWalletService = TransferAgreementAutomation.Worker.Service.IProjectOriginWalletService;
using TransferAgreement = TransferAgreementAutomation.Worker.Models.TransferAgreement;

namespace TransferAgreementAutomation.Worker;

public class TransferAgreementsAutomationWorker : BackgroundService
{
    private readonly ILogger<TransferAgreementsAutomationWorker> logger;
    private readonly ITransferAgreementAutomationMetrics metrics;
    private readonly AutomationCache memoryCache;
    private readonly IDbContextFactory<ApplicationDbContext> contextFactory;
    private readonly IServiceProvider serviceProvider;

    private readonly MemoryCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
    };

    public TransferAgreementsAutomationWorker(
        ILogger<TransferAgreementsAutomationWorker> logger,
        ITransferAgreementAutomationMetrics metrics,
        AutomationCache memoryCache,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IServiceProvider serviceProvider
    )
    {
        this.logger = logger;
        this.metrics = metrics;
        this.memoryCache = memoryCache;
        this.contextFactory = contextFactory;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementsAutomationWorker running at: {time}", DateTimeOffset.Now);
            metrics.ResetCertificatesTransferred();
            metrics.ResetTransferErrors();
            memoryCache.Cache.Set(HealthEntries.Key, HealthEntries.Healthy, cacheOptions);

            using var scope = serviceProvider.CreateScope();
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
                memoryCache.Cache.Set(HealthEntries.Key, HealthEntries.Unhealthy, cacheOptions);
                logger.LogWarning("Something went wrong with the TransferAgreementsAutomationWorker: {exception}", ex);
            }

            scope.Dispose();
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
