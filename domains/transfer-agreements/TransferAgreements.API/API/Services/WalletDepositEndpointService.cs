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
    protected readonly IOptions<ProjectOriginOptions> ProjectOriginOptions;
    protected readonly ILogger<WalletDepositEndpointService> Logger;

    public WalletDepositEndpointService(IOptions<ProjectOriginOptions> grOptions, ILogger<WalletDepositEndpointService> logger)
    {
        ProjectOriginOptions = grOptions;
        Logger = logger;
    }

    public async Task<string> CreateWalletDepositWithToken(JwtToken token)
    {
        var bearerToken = token.GenerateToken();
        var walletDepositEndpoint = await CreateWalletDepositEndpoint(bearerToken);
        return Base64Converter.ConvertWalletDepositEndpointToBase64(walletDepositEndpoint);
    }

    private async Task<WalletDepositEndpoint> CreateWalletDepositEndpoint(string bearerToken)
    {
        using var channel = GrpcChannel.ForAddress(ProjectOriginOptions.Value.WalletUrl);

        var walletServiceClient = new WalletService.WalletServiceClient(channel);
        var request = new CreateWalletDepositEndpointRequest();
        var headers = new Metadata
            { { "Authorization", $"Bearer {bearerToken}" } };
        try
        {
            var response =
                await walletServiceClient.CreateWalletDepositEndpointAsync(request, headers);

            return response.WalletDepositEndpoint;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating WalletDepositEndpoint");
            throw;
        }
    }
}
