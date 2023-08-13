using System;
using System.Threading.Tasks;
using API.Converters;
using API.Options;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.WalletSystem.V1;

namespace API.Services;

public class WalletDepositEndpointService : IWalletDepositEndpointService
{
    private readonly IOptions<ProjectOriginOptions> projectOriginOptions;
    private readonly ILogger<WalletDepositEndpointService> logger;

    public WalletDepositEndpointService(IOptions<ProjectOriginOptions> options, ILogger<WalletDepositEndpointService> logger)
    {
        projectOriginOptions = options;
        this.logger = logger;
    }

    public async Task<string> CreateWalletDepositEndpoint(string bearerToken)
    {
        var walletDepositEndpoint = await GetWalletDepositEndpoint(bearerToken);
        return Base64Converter.ConvertWalletDepositEndpointToBase64(walletDepositEndpoint);
    }

    private async Task<WalletDepositEndpoint> GetWalletDepositEndpoint(string bearerToken)
    {
        using var channel = GrpcChannel.ForAddress(projectOriginOptions.Value.WalletUrl);
        var walletServiceClient = new WalletService.WalletServiceClient(channel);
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
            HandleException(ex, "getting WalletDepositEndpoint");
            throw;
        }
    }

    public async Task<Guid> CreateReceiverDepositEndpoint(string bearerToken, string base64EncodedWalletDepositEndpoint, string receiverTin)
    {
        using var channel = GrpcChannel.ForAddress(projectOriginOptions.Value.WalletUrl);
        var walletServiceClient = new WalletService.WalletServiceClient(channel);

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
            HandleException(ex, "creating ReceiverDepositEndpoint");
            throw;
        }
    }

    private void HandleException(Exception ex, string actionContext)
    {
        if (ex is RpcException rpcEx)
        {
            logger.LogError("Error from WalletService while {ActionContext}: {StatusDetail}", actionContext, rpcEx.Status.Detail);
        }
        else
        {
            logger.LogError(ex, "Error while {ActionContext}", actionContext);
        }
    }
}
