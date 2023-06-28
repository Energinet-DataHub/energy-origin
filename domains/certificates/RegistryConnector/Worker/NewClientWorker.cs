using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.Register.V1;
using ProjectOrigin.WalletSystem.V1;
using Uuid = ProjectOrigin.Register.V1.Uuid;

namespace RegistryConnector.Worker;

public class NewClientWorker : BackgroundService
{
    private readonly WalletService.WalletServiceClient walletServiceClient;
    private readonly ReceiveSliceService.ReceiveSliceServiceClient receiveSliceServiceClient;
    private readonly ILogger<NewClientWorker> logger;

    public NewClientWorker(
        WalletService.WalletServiceClient walletServiceClient,
        ReceiveSliceService.ReceiveSliceServiceClient receiveSliceServiceClient,
        ILogger<NewClientWorker> logger)
    {
        this.walletServiceClient = walletServiceClient;
        this.receiveSliceServiceClient = receiveSliceServiceClient;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var createWalletSectionRequest = new CreateWalletSectionRequest();
            var walletSectionReference = await walletServiceClient.CreateWalletSectionAsync(createWalletSectionRequest);

            logger.LogInformation("Received {response}", walletSectionReference);

            var receiveRequest = new ReceiveRequest
            {
                CertificateId = ToCertId("registryA", Guid.NewGuid()),
                Quantity = 42,
                RandomR = ByteString.CopyFrom(0x01, 0x02, 0x03, 0x04),
                WalletSectionPublicKey = walletSectionReference.SectionPublicKey,
                WalletSectionPosition = 1
            };
            var receiveResponse = await receiveSliceServiceClient.ReceiveSliceAsync(receiveRequest);

            logger.LogInformation("Received {response}", receiveResponse);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Bad");
            logger.LogWarning(ex.Message);
        }
    }

    public static string GenerateToken(string issuer, string audience, string subject, string name, int expirationMinutes = 5)
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

    private static FederatedStreamId ToCertId(string registry, Guid certId) =>
        new() { Registry = registry, StreamId = new Uuid { Value = certId.ToString() } };
}
