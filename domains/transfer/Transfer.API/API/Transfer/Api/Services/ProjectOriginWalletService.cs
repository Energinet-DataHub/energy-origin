using System;
using System.Threading.Tasks;
using API.Shared.Services;
using API.Transfer.Api.Converters;
using API.Transfer.Api.Models;
using API.Transfer.TransferAgreementsAutomation;
using API.Transfer.TransferAgreementsAutomation.Metrics;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;

namespace API.Transfer.Api.Services;

public class ProjectOriginWalletService : ProjectOriginService, IProjectOriginWalletService
{
    private readonly ILogger<ProjectOriginWalletService> logger;
    private readonly WalletService.WalletServiceClient walletServiceClient;
    private readonly ITransferAgreementAutomationMetrics metrics;
    private readonly AutomationCache cache;

    public ProjectOriginWalletService(
        ILogger<ProjectOriginWalletService> logger,
        WalletService.WalletServiceClient walletServiceClient,
        ITransferAgreementAutomationMetrics metrics,
        AutomationCache cache
    )
    {
        this.logger = logger;
        this.walletServiceClient = walletServiceClient;
        this.metrics = metrics;
        this.cache = cache;
    }

    public async Task<string> CreateWalletDepositEndpoint(string bearerToken)
    {
        var walletDepositEndpoint = await GetWalletDepositEndpoint(bearerToken);
        return Base64Converter.ConvertWalletDepositEndpointToBase64(walletDepositEndpoint);
    }

    private async Task<WalletDepositEndpoint> GetWalletDepositEndpoint(string bearerToken)
    {
        var request = new CreateWalletDepositEndpointRequest();
        var headers = new Metadata
        {
            { "Authorization", bearerToken }
        };
        try
        {
            var response = await walletServiceClient.CreateWalletDepositEndpointAsync(request, headers);

            return response.WalletDepositEndpoint;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating WalletDepositEndpoint");
            throw;
        }
    }

    public async Task<Guid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint,
        string receiverTin)
    {
        var headers = new Metadata
        {
            { "Authorization", bearerToken }
        };

        var wde = Base64Converter.ConvertToWalletDepositEndpoint(base64EncodedWalletDepositEndpoint);
        var walletRequest = new CreateReceiverDepositEndpointRequest
        {
            Reference = receiverTin,
            WalletDepositEndpoint = wde
        };
        try
        {
            var response = await walletServiceClient.CreateReceiverDepositEndpointAsync(walletRequest, headers);
            Guid receiverReference = new(response.ReceiverId.Value);

            if (receiverReference == Guid.Empty)
            {
                throw new InvalidOperationException("The receiver Id from the WalletService cannot be an empty Guid.");
            }

            return receiverReference;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating ReceiverDepositEndpoint");
            throw;
        }
    }


    public async Task TransferCertificates(TransferAgreement transferAgreement)
    {
        var header = SetupDummyAuthorizationHeader(transferAgreement.SenderId.ToString());
        var certificates = await GetGranularCertificates(header);

        var certificatesCount = certificates.Count;

        if (certificatesCount == 0)
        {
            logger.LogInformation("No certificates found for {senderId}", transferAgreement.SenderId);
        }

        foreach (var certificate in certificates)
        {
            if (!IsPeriodMatching(transferAgreement, certificate))
            {
                certificatesCount--;
                continue;
            }

            TransferRequest request = new()
            {
                ReceiverId = new Uuid
                {
                    Value = transferAgreement.ReceiverReference.ToString()
                },
                CertificateId = certificate.FederatedId,
                Quantity = certificate.Quantity
            };

            logger.LogInformation("Transferring certificate {certificateId} to {receiver}",
                certificate.FederatedId, transferAgreement.ReceiverTin);

            await walletServiceClient
                .TransferCertificateAsync(request, header);
            SetTransferAttempt(certificate.FederatedId.StreamId.Value);
        }

        metrics.SetNumberOfCertificates(certificatesCount);
    }

    private void SetTransferAttempt(string certificateId)
    {
        var attempt = cache.Cache.Get(certificateId);
        if (attempt == null)
        {
            cache.Cache.Set(certificateId, 1, TimeSpan.FromHours(2));
        }
        else
        {
            metrics.AddTransferError();
        }
    }

    private async Task<RepeatedField<GranularCertificate>> GetGranularCertificates(Metadata headers)
    {
        var response = await walletServiceClient.QueryGranularCertificatesAsync(new QueryRequest(), headers);
        return response.GranularCertificates;
    }

    private static bool IsPeriodMatching(TransferAgreement transferAgreement, GranularCertificate certificate)
    {
        if (transferAgreement.EndDate == null)
        {
            return certificate.Type == GranularCertificateType.Production &&
                   certificate.Start >= Timestamp.FromDateTimeOffset(transferAgreement.StartDate);
        }

        return certificate.Type == GranularCertificateType.Production &&

                   certificate.Start >= Timestamp.FromDateTimeOffset(transferAgreement.StartDate) &&
                   certificate.End <= Timestamp.FromDateTimeOffset(transferAgreement.EndDate!.Value)
               ;
    }
}
