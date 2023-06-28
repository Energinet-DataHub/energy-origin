using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.WalletSystem.V1;

namespace RegistryConnector.Worker;

public class NewClientWorker : BackgroundService
{
    private readonly ILogger<NewClientWorker> logger;
    private readonly ECDsa ecdsa;

    public NewClientWorker(ILogger<NewClientWorker> logger)
    {
        this.logger = logger;
        ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:7890");

            var headers = new Metadata();
            headers.Add("Authorization", $"Bearer {GenerateToken("issuer", "aud", Guid.NewGuid().ToString(), "foo")}");

            var walletServiceClient = new WalletService.WalletServiceClient(channel);

            var walletSectionReference = await walletServiceClient.CreateWalletSectionAsync(new CreateWalletSectionRequest(), headers);

            logger.LogInformation("Received {response}", walletSectionReference);

            //var receiveSliceServiceClient = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

            //var response = await receiveSliceServiceClient.ReceiveSliceAsync(new ReceiveRequest());

            //logger.LogInformation("Received {response}", response);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Bad");
            logger.LogWarning(ex.Message);
        }
    }

    private string GenerateToken(string issuer, string audience, string subject, string name, int expirationMinutes = 5)
    {
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
}
