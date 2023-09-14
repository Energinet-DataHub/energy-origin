using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Metrics;
using API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace API.TransferAgreementsAutomation;

public class TransferAgreementsAutomationService : ITransferAgreementsAutomationService
{
    private readonly ILogger<TransferAgreementsAutomationService> logger;
    private readonly ITransferAgreementRepository transferAgreementRepository;
    private readonly IProjectOriginWalletService projectOriginWalletService;
    private readonly MyCache memoryCache;
    private readonly ITransferAgreementAutomationMetrics metrics;

    private MemoryCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
        Size = 1
    };
    public TransferAgreementsAutomationService(
        ILogger<TransferAgreementsAutomationService> logger,
        ITransferAgreementRepository transferAgreementRepository,
        IProjectOriginWalletService projectOriginWalletService,
        MyCache memoryCache,
        ITransferAgreementAutomationMetrics metrics
        )
    {
        this.logger = logger;
        this.transferAgreementRepository = transferAgreementRepository;
        this.projectOriginWalletService = projectOriginWalletService;
        this.memoryCache = memoryCache;
        this.metrics = metrics;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {

            logger.LogInformation("TransferAgreementsAutomationService running at: {time}", DateTimeOffset.Now);
            metrics.ResetCertificatesTransferred();

            try
            {
                var transferAgreements = await transferAgreementRepository.GetAllTransferAgreements();
                metrics.SetNumberOfTransferAgreements(transferAgreements.Count);

                foreach (var transferAgreement in transferAgreements)
                {
                    await projectOriginWalletService.TransferCertificates(transferAgreement);
                }
                memoryCache.Cache.Set(CacheValues.Key, CacheValues.Success, cacheOptions);
            }
            catch (Exception e)
            {
                memoryCache.Cache.Set(CacheValues.Key, CacheValues.Error, cacheOptions);
                logger.LogWarning("Something went wrong with the TransferAgreementsAutomationService: {exception}", e);
            }

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
