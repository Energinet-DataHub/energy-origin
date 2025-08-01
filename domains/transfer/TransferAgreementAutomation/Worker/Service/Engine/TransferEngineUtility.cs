using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using TransferAgreementAutomation.Worker.Service.TransactionStatus;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using RequestStatus = EnergyOrigin.WalletClient.RequestStatus;

namespace TransferAgreementAutomation.Worker.Service.Engine;

public class TransferEngineUtility
{
    public int BatchSize { get; init; } = 1000;
    public int StatusUpdateIntervalMinutes { get; } = 1;
    public int StatusUpdateTimeoutMinutes { get; } = 120;
    public int StatusUpdateDeleteMinutes { get; } = 24 * 60;
    public bool IsTrial { get; set; }

    private readonly IWalletClient _walletClient;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly ILogger<TransferEngineUtility> _logger;

    public TransferEngineUtility(IWalletClient walletClient, IRequestStatusRepository requestStatusRepository,
        ILogger<TransferEngineUtility> logger)
    {
        _walletClient = walletClient;
        _requestStatusRepository = requestStatusRepository;
        _logger = logger;
    }

    public async Task<List<GranularCertificate>> GetProductionCertificates(OrganizationId organizationId, CancellationToken cancellationToken)
    {
        var hasMoreCertificates = true;
        var certificates = new List<GranularCertificate>();
        while (hasMoreCertificates)
        {
            var response = await _walletClient.GetGranularCertificatesAsync(organizationId.Value, cancellationToken, limit: BatchSize,
                skip: certificates.Count, CertificateType.Production);

            if (response == null)
            {
                throw new TransferCertificatesException(
                    $"Something went wrong when getting certificates from the wallet for {organizationId.Value}. Response is null.");
            }

            certificates.AddRange(response.Result);
            if (certificates.Count >= response.Metadata.Total)
            {
                hasMoreCertificates = false;
            }
        }

        return certificates.Where(cert => IsTrial == cert.IsTrialCertificate).ToList();
    }

    public async Task<List<GranularCertificate>> GetCertificates(OrganizationId organizationId, CancellationToken cancellationToken)
    {
        var hasMoreCertificates = true;
        var certificates = new List<GranularCertificate>();
        while (hasMoreCertificates)
        {
            var response = await _walletClient.GetGranularCertificatesAsync(organizationId.Value, cancellationToken, limit: BatchSize,
                skip: certificates.Count);

            if (response == null)
            {
                throw new TransferCertificatesException(
                    $"Something went wrong when getting certificates from the wallet for {organizationId.Value}. Response is null.");
            }

            certificates.AddRange(response.Result);
            if (certificates.Count >= response.Metadata.Total)
            {
                hasMoreCertificates = false;
            }
        }

        return certificates.Where(cert => IsTrial == cert.IsTrialCertificate).ToList();
    }

    public async Task<bool> HasPendingTransactions(OrganizationId organizationId, CancellationToken cancellationToken = default)
    {
        var transactionRequests = await _requestStatusRepository.GetByOrganization(organizationId, cancellationToken);

        _logger.LogInformation("Tracking {TransactionCount} transactions for organization {OrganizationId}", transactionRequests.Count, organizationId);

        await UpdateStatusFromWallet(organizationId, cancellationToken, transactionRequests);
        return transactionRequests.Any(t => t.Status == Status.Pending);
    }

    private async Task UpdateStatusFromWallet(OrganizationId organizationId, CancellationToken cancellationToken,
        IList<TransactionStatus.RequestStatus> transactionRequests)
    {
        var transactionsToUpdate = transactionRequests
            .Where(rs => rs.StatusTimestamp < UnixTimestamp.Now().AddMinutes(-StatusUpdateIntervalMinutes))
            .ToList();

        foreach (var transactionToUpdate in transactionsToUpdate)
        {
            if (transactionToUpdate.RequestTimestamp < UnixTimestamp.Now().AddMinutes(-StatusUpdateDeleteMinutes))
            {
                await _requestStatusRepository.Delete(transactionToUpdate.Id, cancellationToken);
                transactionToUpdate.UpdateStatus(Status.Timeout);
            }
            else if (transactionToUpdate.RequestTimestamp < UnixTimestamp.Now().AddMinutes(-StatusUpdateTimeoutMinutes))
            {
                transactionToUpdate.UpdateStatus(Status.Timeout);
                await _requestStatusRepository.Update(transactionToUpdate, cancellationToken);
            }
            else
            {
                var updatedStatus = await _walletClient.GetRequestStatusAsync(organizationId.Value, transactionToUpdate.RequestId, cancellationToken);
                transactionToUpdate.UpdateStatus(MapStatus(updatedStatus));
                await _requestStatusRepository.Update(transactionToUpdate, cancellationToken);
            }
        }
    }

    private Status MapStatus(RequestStatus updatedStatus)
    {
        return updatedStatus switch
        {
            RequestStatus.Completed => Status.Completed,
            RequestStatus.Failed => Status.Failed,
            RequestStatus.Pending => Status.Pending,
            _ => throw new InvalidOperationException("Unknown request status")
        };
    }
}
