using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.TransferAgreementsAutomation.Metrics;
using API.Transfer.TransferAgreementsAutomation.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Transfer.TransferAgreementsAutomation;

public class TransferAgreementsAutomationWorker : BackgroundService
{
    private readonly ILogger<TransferAgreementsAutomationWorker> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly ITransferAgreementAutomationMetrics metrics;
    private readonly AutomationCache memoryCache;

    private readonly MemoryCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
    };

    public TransferAgreementsAutomationWorker(
        ILogger<TransferAgreementsAutomationWorker> logger,
        IServiceProvider serviceProvider,
        ITransferAgreementAutomationMetrics metrics,
        AutomationCache memoryCache
    )
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.metrics = metrics;
        this.memoryCache = memoryCache;
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
            var transferAgreementsAutomationService = scope.ServiceProvider.GetRequiredService<ITransferAgreementsAutomationService>();

            try
            {
                await transferAgreementsAutomationService.Run(stoppingToken);
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

    private async Task SleepToNearestHour(CancellationToken cancellationToken)
    {
        var minutesToNextHour = 60 - DateTimeOffset.Now.Minute;
        logger.LogInformation("Sleeping until next full hour {minutesToNextHour}", minutesToNextHour);
        await Task.Delay(TimeSpan.FromMinutes(minutesToNextHour), cancellationToken);
    }
}
