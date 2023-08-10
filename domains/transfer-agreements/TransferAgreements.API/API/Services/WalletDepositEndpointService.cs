using System;
using System.Threading.Tasks;
using API.Converters;
using API.Data;
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

    public async Task<string> CreateWalletDepositWithToken(JwtToken token)
    {
        var bearerToken = token.GenerateToken();
        var walletDepositEndpoint = await GetWalletDepositEndpoint(bearerToken);
        return Base64Converter.ConvertWalletDepositEndpointToBase64(walletDepositEndpoint);
    }

    public async Task<WalletDepositEndpoint> GetWalletDepositEndpoint(string bearerToken)
    {
        using var channel = GrpcChannel.ForAddress(projectOriginOptions.Value.WalletUrl);
        var walletServiceClient = new WalletService.WalletServiceClient(channel);
        var request = new CreateWalletDepositEndpointRequest();
        var headers = new Metadata
            {
                { "Authorization", $"Bearer {bearerToken}" }
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
        using var channel = GrpcChannel.ForAddress(projectOriginOptions.Value.WalletUrl);
        var walletServiceClient = new WalletService.WalletServiceClient(channel);

        var headers = new Metadata
            { { "Authorization", $"Bearer {bearerToken}" } };
        var wde = Base64Converter.ConvertToWalletDepositEndpoint(base64EncodedWalletDepositEndpoint);
        var walletRequest = new CreateReceiverDepositEndpointRequest
        {
            Reference = receiverTin,
            WalletDepositEndpoint = wde
        };

        var response = await walletServiceClient.CreateReceiverDepositEndpointAsync(walletRequest, headers);

        Guid receiverReference = new(response.ReceiverId.Value);

        if (receiverReference == Guid.Empty)
        {
            throw new ArgumentException("The receiver Id cannot be an empty Guid.", nameof(response.ReceiverId));
        }

        return receiverReference;
    }
}
