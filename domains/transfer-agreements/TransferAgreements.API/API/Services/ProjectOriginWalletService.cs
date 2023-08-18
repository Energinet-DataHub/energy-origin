using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Converters;
using API.Data;
using API.Models;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;

namespace API.Services;

public class ProjectOriginWalletService : IProjectOriginWalletService
{
    private readonly ILogger<ProjectOriginWalletService> logger;
    private readonly WalletService.WalletServiceClient walletServiceClient;

    public ProjectOriginWalletService(
        ILogger<ProjectOriginWalletService> logger,
        WalletService.WalletServiceClient walletServiceClient
    )
    {
        this.logger = logger;
        this.walletServiceClient = walletServiceClient;
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

    public async Task<Guid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin)
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

        if (certificates.Count == 0)
        {
            logger.LogInformation("No certificates found for {senderId}", transferAgreement.SenderId);
            return;
        }

        foreach (var certificate in certificates)
        {
            if (IsPeriodNotMatching(transferAgreement, certificate)) continue;

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
    }

    private async Task<RepeatedField<GranularCertificate>> GetGranularCertificates(Metadata headers)
    {
        var response = await walletServiceClient.QueryGranularCertificatesAsync(new QueryRequest(), headers);
        return response.GranularCertificates;
    }

    private static bool IsPeriodNotMatching(TransferAgreement transferAgreement, GranularCertificate certificate)
    {
        if (transferAgreement.EndDate == null)
        {
            return certificate.Type != GranularCertificateType.Production ||
                   certificate.End < Timestamp.FromDateTimeOffset(transferAgreement.StartDate);
        }

        return certificate.Type != GranularCertificateType.Production ||
               (
                   certificate.Start < Timestamp.FromDateTimeOffset(transferAgreement.StartDate) &&
                   certificate.End > Timestamp.FromDateTimeOffset(transferAgreement.EndDate!.Value)
               );
    }

    private static Metadata SetupDummyAuthorizationHeader(string owner)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", owner) }),
            Expires = DateTime.UtcNow.AddDays(7),
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new Metadata
        {
            { "Authorization", tokenHandler.WriteToken(token) }
        };
    }
}
