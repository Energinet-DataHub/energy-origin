using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Metrics;
using API.Services;
using Microsoft.Extensions.Logging;

namespace API.TransferAgreementsAutomation;

public class TransferAgreementsAutomationService : ITransferAgreementsAutomationService
{
    private readonly ILogger<TransferAgreementsAutomationService> logger;
    private readonly ITransferAgreementRepository transferAgreementRepository;
    private readonly IProjectOriginWalletService projectOriginWalletService;
    private readonly ITransferAgreementAutomationMetrics metrics;

    public TransferAgreementsAutomationService(
        ILogger<TransferAgreementsAutomationService> logger,
        ITransferAgreementRepository transferAgreementRepository,
        IProjectOriginWalletService projectOriginWalletService,
        ITransferAgreementAutomationMetrics metrics
        )
    {
        this.logger = logger;
        this.transferAgreementRepository = transferAgreementRepository;
        this.projectOriginWalletService = projectOriginWalletService;
        this.metrics = metrics;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("TransferAgreementsAutomationService running at: {time}", DateTimeOffset.Now);
            metrics.ResetCertificatesTransferred();
            var transferAgreements = await transferAgreementRepository.GetAllTransferAgreements();

            foreach (var transferAgreement in transferAgreements)
            {
                await projectOriginWalletService.TransferCertificates(transferAgreement);
            }

            metrics.SetNumberOfTransferAgreementsOnLastRun(transferAgreements.Count);

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
