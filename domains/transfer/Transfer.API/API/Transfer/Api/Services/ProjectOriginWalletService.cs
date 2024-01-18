using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.Transfer.Api.Converters;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProjectOrigin.WalletSystem.V1;

namespace API.Transfer.Api.Services;

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

    public async Task<string> CreateWalletDepositEndpoint(AuthenticationHeaderValue bearerToken)
    {
        var walletDepositEndpoint = await GetWalletDepositEndpoint(bearerToken);
        return Base64Converter.ConvertWalletDepositEndpointToBase64(walletDepositEndpoint);
    }

    private async Task<WalletDepositEndpoint> GetWalletDepositEndpoint(AuthenticationHeaderValue bearerToken)
    {
        var request = new CreateWalletDepositEndpointRequest();
        var headers = new Metadata
        {
            { "Authorization", bearerToken.ToString() }
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

    public async Task<Guid> CreateReceiverDepositEndpoint(AuthenticationHeaderValue bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin)
    {
        var headers = new Metadata
        {
            { "Authorization", bearerToken.ToString() }
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
}
