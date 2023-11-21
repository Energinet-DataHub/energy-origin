using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementsAutomation.Metrics;

namespace API.Transfer.TransferAgreementsAutomation.Service;

public class TransferAgreementsAutomationService : ITransferAgreementsAutomationService
{
    private readonly ITransferAgreementRepository transferAgreementRepository;
    private readonly IProjectOriginWalletService projectOriginWalletService;
    private readonly ITransferAgreementAutomationMetrics metrics;

    public TransferAgreementsAutomationService(
        ITransferAgreementRepository transferAgreementRepository,
        IProjectOriginWalletService projectOriginWalletService,
        ITransferAgreementAutomationMetrics metrics
    )
    {
        this.transferAgreementRepository = transferAgreementRepository;
        this.projectOriginWalletService = projectOriginWalletService;
        this.metrics = metrics;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        var transferAgreements = await transferAgreementRepository.GetAllTransferAgreements();
        metrics.SetNumberOfTransferAgreements(transferAgreements.Count);

        foreach (var transferAgreement in transferAgreements)
        {
            await projectOriginWalletService.TransferCertificates(transferAgreement);
        }
    }
}
