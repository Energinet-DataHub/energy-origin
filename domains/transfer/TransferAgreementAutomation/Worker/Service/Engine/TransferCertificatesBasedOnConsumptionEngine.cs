using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using TransferAgreementAutomation.Worker.Service.TransactionStatus;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using RequestStatus = TransferAgreementAutomation.Worker.Service.TransactionStatus.RequestStatus;

namespace TransferAgreementAutomation.Worker.Service.Engine;

public class TransferCertificatesBasedOnConsumptionEngine : ITransferEngine
{
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly TransferEngineUtility _transferUtility;
    private readonly ILogger<TransferCertificatesBasedOnConsumptionEngine> _logger;
    private readonly IWalletClient _walletClient;

    public TransferCertificatesBasedOnConsumptionEngine(IRequestStatusRepository requestStatusRepository, TransferEngineUtility transferUtility,
        ILogger<TransferCertificatesBasedOnConsumptionEngine> logger, IWalletClient walletClient)
    {
        _requestStatusRepository = requestStatusRepository;
        _transferUtility = transferUtility;
        _logger = logger;
        _walletClient = walletClient;
    }

    public bool IsSupported(TransferAgreement transferAgreement)
    {
        return transferAgreement.Type == TransferAgreementType.TransferCertificatesBasedOnConsumption;
    }

    public async Task TransferCertificates(TransferAgreement transferAgreement, CancellationToken cancellationToken = default)
    {
        if (!IsSupported(transferAgreement))
        {
            throw new ArgumentException(nameof(transferAgreement));
        }

        var senderOrganizationId = transferAgreement.SenderId;
        var receiverOrganizationId = transferAgreement.ReceiverId;

        if (receiverOrganizationId is null)
        {
            _logger.LogInformation("Skipping consumption based transfer agreement {Id}, sender id is not specified", transferAgreement.Id);
            return;
        }

        if (await _transferUtility.HasPendingTransactions(senderOrganizationId, cancellationToken))
        {
            _logger.LogInformation("Skipping transfer agreement {Id}, sender {OrgId} has pending transactions", transferAgreement.Id,
                senderOrganizationId);
            return;
        }

        if (await _transferUtility.HasPendingTransactions(receiverOrganizationId, cancellationToken))
        {
            _logger.LogInformation("Skipping transfer agreement {Id}, receiver {OrgId} has pending transactions", transferAgreement.Id,
                receiverOrganizationId);
            return;
        }

        var receiverUnmatchedConsumptionGroupedByPeriod = await GetUnmatchedReceiverConsumptionGroupedByPeriod(receiverOrganizationId);
        if (receiverUnmatchedConsumptionGroupedByPeriod.Count == 0)
        {
            _logger.LogInformation("Skipping transfer agreement {Id}, receiver {OrgId} has no unmatched consumption", transferAgreement.Id,
                receiverOrganizationId);
            return;
        }

        var senderProductionCertificatesGroupedByPeriod = await GetSenderProductionCertificatesGroupedByPeriod(senderOrganizationId);

        foreach (var receiverConsumptionPeriod in receiverUnmatchedConsumptionGroupedByPeriod)
        {
            if (!IsPeriodIncluded(transferAgreement, receiverConsumptionPeriod.Period))
            {
                continue;
            }

            var senderCertificatesInPeriod =
                GetProductionCertificatesInPeriod(senderProductionCertificatesGroupedByPeriod, receiverConsumptionPeriod);

            var senderCertificatesToTransfer = SelectSenderCertificatesForTransfer(receiverConsumptionPeriod.Quantity, senderCertificatesInPeriod);

            await TransferCertificates(transferAgreement, cancellationToken, senderCertificatesToTransfer, senderOrganizationId,
                receiverOrganizationId);
        }
    }

    private static List<GranularCertificate> GetProductionCertificatesInPeriod(
        List<IGrouping<Period, GranularCertificate>> senderProductionCertificatesGroupedByPeriod, ConsumptionPeriod receiverConsumptionPeriod)
    {
        var senderCertificatesInPeriod = senderProductionCertificatesGroupedByPeriod
            .Where(senderGroup => senderGroup.Key == receiverConsumptionPeriod.Period)
            .SelectMany(c => c)
            .ToList();

        return senderCertificatesInPeriod;
    }

    private async Task TransferCertificates(TransferAgreement transferAgreement, CancellationToken cancellationToken,
        List<CertificatesToTransfer> senderCertificatesToTransfer, OrganizationId senderOrganizationId, OrganizationId receiverOrganizationId)
    {
        foreach (var senderProductionCertificate in senderCertificatesToTransfer)
        {
            _logger.LogInformation("Transferring {quantity} from certificate {CertificateId} to {OrganizationId}",
                senderProductionCertificate.Quantity, senderProductionCertificate.Certificate.FederatedStreamId, receiverOrganizationId);

            var requestId = await _walletClient.TransferCertificatesAsync(senderOrganizationId.Value, senderProductionCertificate.Certificate,
                senderProductionCertificate.Quantity, transferAgreement.ReceiverReference, cancellationToken);

            await _requestStatusRepository.Add(
                new RequestStatus(senderOrganizationId, receiverOrganizationId, requestId.TransferRequestId, UnixTimestamp.Now()), cancellationToken);
        }
    }

    private async Task<List<IGrouping<Period, GranularCertificate>>> GetSenderProductionCertificatesGroupedByPeriod(
        OrganizationId senderOrganizationId)
    {
        var senderCertificates = await _transferUtility.GetProductionCertificates(senderOrganizationId);
        var senderProductionCertificatesGroupedByPeriod = GetProductionCertificatesGroupedByPeriod(senderCertificates);
        _logger.LogInformation("Found {Count} periods where sender has production certificates", senderProductionCertificatesGroupedByPeriod.Count);

        return senderProductionCertificatesGroupedByPeriod;
    }

    private async Task<List<ConsumptionPeriod>> GetUnmatchedReceiverConsumptionGroupedByPeriod(OrganizationId receiverOrganizationId)
    {
        var receiverCertificates = await _transferUtility.GetCertificates(receiverOrganizationId);
        _logger.LogInformation("Found {Count} certificates for receiver", receiverCertificates.Count);
        var receiverUnmatchedConsumptionGroupedByPeriod = GetUnmatchedConsumptionGroupedByPeriod(receiverCertificates);
        _logger.LogInformation("Found {Count} periods where receiver has unmatched consumption", receiverUnmatchedConsumptionGroupedByPeriod.Count);

        return receiverUnmatchedConsumptionGroupedByPeriod;
    }

    private List<CertificatesToTransfer> SelectSenderCertificatesForTransfer(uint requestedQuantity,
        List<GranularCertificate> senderCertificatesInPeriod)
    {
        uint selectedQuantity = 0;
        return senderCertificatesInPeriod.Select(c =>
        {
            var selectedCertificateQuantity = selectedQuantity >= requestedQuantity ? 0 : Math.Min(requestedQuantity - selectedQuantity, c.Quantity);
            selectedQuantity += selectedCertificateQuantity;
            return new CertificatesToTransfer(c, selectedCertificateQuantity);
        }).TakeWhile(c => c.Quantity > 0).ToList();
    }

    private List<ConsumptionPeriod> GetUnmatchedConsumptionGroupedByPeriod(List<GranularCertificate> certificates)
    {
        return certificates.GroupBy(c => new Period(c.Start, c.End))
            .Select(g => new ConsumptionPeriod(g.Key, UnmatchedConsumptionInPeriod(g)))
            .Where(p => p.Quantity > 0)
            .ToList();
    }

    private List<IGrouping<Period, GranularCertificate>> GetProductionCertificatesGroupedByPeriod(List<GranularCertificate> certificates)
    {
        return certificates.Where(c => c.CertificateType == CertificateType.Production)
            .GroupBy(c => new Period(c.Start, c.End))
            .ToList();
    }

    private uint UnmatchedConsumptionInPeriod(IGrouping<Period, GranularCertificate> periodGrouping)
    {
        uint consumptionInPeriod = 0;
        uint productionInPeriod = 0;
        foreach (var certificate in periodGrouping)
        {
            if (certificate.CertificateType == CertificateType.Consumption)
            {
                consumptionInPeriod += certificate.Quantity;
            }
            else if (certificate.CertificateType == CertificateType.Production)
            {
                productionInPeriod += certificate.Quantity;
            }
            else
            {
                throw new InvalidOperationException("Unknown certificate type");
            }
        }

        return consumptionInPeriod > productionInPeriod ? consumptionInPeriod - productionInPeriod : 0;
    }

    private static bool IsPeriodIncluded(TransferAgreement transferAgreement, Period period)
    {
        if (transferAgreement.EndDate == null)
        {
            return period.Start >= transferAgreement.StartDate.EpochSeconds;
        }

        return period.Start >= transferAgreement.StartDate.EpochSeconds &&
               period.End <= transferAgreement.EndDate!.EpochSeconds;
    }

    private record Period(long Start, long End);

    private record ConsumptionPeriod(Period Period, uint Quantity);

    private record CertificatesToTransfer(GranularCertificate Certificate, uint Quantity);
}
