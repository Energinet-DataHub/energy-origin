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
    protected readonly IOptions<GrpcOptions> ProjectOriginOptions;
    protected readonly ILogger<WalletDepositEndpointService> Logger;  // Specify the type here

    public WalletDepositEndpointService(IOptions<GrpcOptions> grOptions, ILogger<WalletDepositEndpointService> logger) // And also here
    {
        ProjectOriginOptions = grOptions;
        Logger = logger;
    }

    public async Task<string> CreateWalletDepositWithToken(string issuer, string audience, string subject, string name, int expirationMinutes = 5)
    {
        var token = GenerateToken(issuer, audience, subject, name, expirationMinutes);
        var walletDepositEndpoint = await CreateWalletDepositEndpoint(token);
        return ConvertObjectToBase64(walletDepositEndpoint);
    }

    private static string GenerateToken(string issuer, string audience, string subject, string name, int expirationMinutes = 5)
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var claims = new[]
        {
            new Claim("sub", subject),
            new Claim("name", name),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var key = new ECDsaSecurityKey(ecdsa);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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
            Logger.LogError(ex, "Error creating WalletDepositEndpoint");  // Use the Logger to log any exceptions
            throw;
        }
    }
}
