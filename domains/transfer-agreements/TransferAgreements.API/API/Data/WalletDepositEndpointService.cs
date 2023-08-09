using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.WalletSystem.V1;

namespace API.Data;

public class WalletDepositEndpointService : IWalletDepositEndpointService
{
    protected readonly IOptions<ProjectOriginOptions> ProjectOriginOptions;
    protected readonly ILogger<WalletDepositEndpointService> Logger;

    public WalletDepositEndpointService(IOptions<ProjectOriginOptions> grOptions, ILogger<WalletDepositEndpointService> logger)
    {
        ProjectOriginOptions = grOptions;
        Logger = logger;
    }

    public async Task<string> CreateWalletDepositWithToken(string jwtToken)
    {
        var walletDepositEndpoint = await CreateWalletDepositEndpoint(jwtToken);
        return ConvertObjectToBase64(walletDepositEndpoint);
    }

    private static string ConvertObjectToBase64(object obj)
    {
        var jsonString = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var base64String = Convert.ToBase64String(bytes);

        return base64String;
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
