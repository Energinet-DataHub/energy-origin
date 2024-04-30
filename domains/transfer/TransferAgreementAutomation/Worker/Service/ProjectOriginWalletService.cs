using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;
using Microsoft.Extensions.Logging;
using ProjectOriginClients;
using ProjectOriginClients.Models;
using TransferAgreementAutomation.Worker.Metrics;

namespace TransferAgreementAutomation.Worker.Service;

public class ProjectOriginWalletService : IProjectOriginWalletService
{
    private readonly ILogger<ProjectOriginWalletService> logger;
    private readonly IProjectOriginWalletClient walletClient;
    private readonly ITransferAgreementAutomationMetrics metrics;

    public ProjectOriginWalletService(
        ILogger<ProjectOriginWalletService> logger,
        IProjectOriginWalletClient walletClient,
        ITransferAgreementAutomationMetrics metrics
    )
    {
        this.logger = logger;
        this.walletClient = walletClient;
        this.metrics = metrics;
    }

    public async Task TransferCertificates(TransferAgreement transferAgreement)
    {
        logger.LogInformation("Getting certificates for {senderId}", transferAgreement.SenderId);

        var certificates = await walletClient.GetGranularCertificates(transferAgreement.SenderId, new CancellationToken());

        if (certificates == null || !certificates.Result.Any())
        {
            logger.LogInformation("No certificates found for {senderId}", transferAgreement.SenderId);
        }

        var certificatesCount = certificates!.Result.Count();

        logger.LogInformation("Found {certificatesCount} certificates to transfer for transfer agreement with id {id}", certificatesCount, transferAgreement.Id);

        foreach (var certificate in certificates.Result)
        {
            if (!IsPeriodMatching(transferAgreement, certificate))
            {
                certificatesCount--;
                continue;
            }

            logger.LogInformation("Transferring certificate {certificateId} to {receiver}",
                certificate.FederatedStreamId, transferAgreement.ReceiverTin);

            await walletClient.TransferCertificates(transferAgreement.SenderId, certificate, certificate.Quantity, transferAgreement.ReceiverReference);
        }

        metrics.SetNumberOfCertificates(certificatesCount);
    }

    private static bool IsPeriodMatching(TransferAgreement transferAgreement, GranularCertificate certificate)
    {
        if (transferAgreement.EndDate == null)
        {
            return certificate.CertificateType == CertificateType.Production &&
                   certificate.Start >= transferAgreement.StartDate.ToUnixTimeSeconds();
        }

        return certificate.CertificateType == CertificateType.Production &&
               certificate.Start >= transferAgreement.StartDate.ToUnixTimeSeconds() &&
               certificate.End <= transferAgreement.EndDate!.Value.ToUnixTimeSeconds();
    }
}
