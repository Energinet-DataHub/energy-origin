using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using DataContext.Models;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;
using TransferAgreementAutomation.Worker.Metrics;
using Claim = System.Security.Claims.Claim;

namespace TransferAgreementAutomation.Worker.Service;

public class ProjectOriginWalletService : IProjectOriginWalletService
{
    private readonly ILogger<ProjectOriginWalletService> logger;
    private readonly WalletService.WalletServiceClient walletServiceClient;
    private readonly ITransferAgreementAutomationMetrics metrics;

    public ProjectOriginWalletService(
        ILogger<ProjectOriginWalletService> logger,
        WalletService.WalletServiceClient walletServiceClient,
        ITransferAgreementAutomationMetrics metrics
    )
    {
        this.logger = logger;
        this.walletServiceClient = walletServiceClient;
        this.metrics = metrics;
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
        }

        metrics.SetNumberOfCertificates(certificatesCount);
    }

    private static Metadata SetupDummyAuthorizationHeader(string owner)
    {
        return new Metadata
        {
            { "Authorization", $"Bearer {GenerateBearerToken(owner)}" }
        };
    }

    private static string GenerateBearerToken(string owner)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", owner) }),
            Expires = DateTime.UtcNow.AddDays(7),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
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
                   certificate.Start >=
                   Timestamp.FromDateTimeOffset(transferAgreement.StartDate);
        }

        return certificate.Type == GranularCertificateType.Production &&
               certificate.Start >=
               Timestamp.FromDateTimeOffset(transferAgreement.StartDate) &&
               certificate.End <=
               Timestamp.FromDateTimeOffset(transferAgreement.EndDate!.Value)
            ;
    }
}
