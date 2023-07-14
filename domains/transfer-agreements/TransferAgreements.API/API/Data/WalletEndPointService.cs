using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.WalletSystem.V1;

namespace API.Data;

public class WalletEndPointService : IWalletEndPointService
{
    protected readonly IOptions<GrpcOptions> ProjectOriginOptions;
    protected readonly ILogger Logger;
    public WalletEndPointService(IOptions<GrpcOptions> grOptions, ILogger logger)
    {
        ProjectOriginOptions = grOptions;
        Logger = logger;
    }

    protected static string GenerateToken(string issuer, string audience, string subject, string name, int expirationMinutes = 5)
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
    protected async Task<WalletDepositEndpoint> CreateWalletDepositEndpoint(string bearerToken)
    {
        using var channel = GrpcChannel.ForAddress(ProjectOriginOptions.Value.WalletUrl);

        var walletServiceClient = new WalletService.WalletServiceClient(channel);
        var request = new CreateWalletDepositEndpointRequest();
        var headers = new Metadata
            { { "Authorization", $"Bearer {bearerToken}" } };
        var response =
            await walletServiceClient.CreateWalletDepositEndpointAsync(request, headers);

        return response.WalletDepositEndpoint;
    }
}
