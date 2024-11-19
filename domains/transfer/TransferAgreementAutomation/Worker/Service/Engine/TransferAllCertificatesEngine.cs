using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using ProjectOriginClients;
using ProjectOriginClients.Models;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Service.TransactionStatus;
using RequestStatus = TransferAgreementAutomation.Worker.Service.TransactionStatus.RequestStatus;

namespace TransferAgreementAutomation.Worker.Service.Engine;

public class TransferAllCertificatesEngine : ITransferEngine
{
    private readonly IRequestStatusRepository requestStatusRepository;
    private readonly ILogger<TransferAllCertificatesEngine> logger;
    private readonly IProjectOriginWalletClient walletClient;
    private readonly ITransferAgreementAutomationMetrics metrics;
    private readonly TransferEngineUtility transferUtility;

    public TransferAllCertificatesEngine(
        IRequestStatusRepository requestStatusRepository,
        ILogger<TransferAllCertificatesEngine> logger,
        IProjectOriginWalletClient walletClient,
        ITransferAgreementAutomationMetrics metrics,
        TransferEngineUtility transferUtility
    )
    {
        this.requestStatusRepository = requestStatusRepository;
        this.logger = logger;
        this.walletClient = walletClient;
        this.metrics = metrics;
        this.transferUtility = transferUtility;
    }

    public bool IsSupported(TransferAgreement transferAgreement)
    {
        return transferAgreement.Type == TransferAgreementType.TransferAllCertificates;
    }

    public async Task TransferCertificates(TransferAgreement transferAgreement, CancellationToken cancellationToken = default)
    {
        if (!IsSupported(transferAgreement))
        {
            throw new ArgumentException(nameof(transferAgreement));
        }

        if (await transferUtility.HasPendingTransactions(transferAgreement.SenderId, cancellationToken))
        {
            logger.LogInformation("Skipping transfer agreement {Id}, sender {OrgId} has pending transactions", transferAgreement.Id,
                transferAgreement.SenderId);
            return;
        }

        logger.LogInformation("Getting certificates for {senderId}", transferAgreement.SenderId);

        var certificates = await transferUtility.GetCertificates(transferAgreement.SenderId);

        logger.LogInformation("Found {certificatesCount} certificates to transfer for transfer agreement with id {id}", certificates.Count,
            transferAgreement.Id);

        var certificatesCount = certificates.Count();
        foreach (var certificate in certificates)
        {
            if (!IsPeriodMatching(transferAgreement, certificate))
            {
                certificatesCount--;
                continue;
            }

            logger.LogInformation("Transferring certificate {certificateId} to {receiver}",
                certificate.FederatedStreamId, transferAgreement.ReceiverTin);

            var transactionRequestId = await walletClient.TransferCertificates(transferAgreement.SenderId.Value, certificate, certificate.Quantity,
                transferAgreement.ReceiverReference);
            await requestStatusRepository.Add(
                new RequestStatus(transferAgreement.SenderId, transferAgreement.ReceiverId ?? OrganizationId.Empty(), transactionRequestId.TransferRequestId, UnixTimestamp.Now()),
                cancellationToken);
        }

        metrics.SetNumberOfCertificates(certificatesCount);
    }

    private static bool IsPeriodMatching(TransferAgreement transferAgreement, GranularCertificate certificate)
    {
        if (transferAgreement.EndDate == null)
        {
            return certificate.CertificateType == CertificateType.Production &&
                   certificate.Start >= transferAgreement.StartDate.EpochSeconds;
        }

        return certificate.CertificateType == CertificateType.Production &&
               certificate.Start >= transferAgreement.StartDate.EpochSeconds &&
               certificate.End <= transferAgreement.EndDate!.EpochSeconds;
    }
}
