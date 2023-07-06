using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.WalletSystem.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Google.Protobuf;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.PedersenCommitment;
using System.Collections.Generic;
using System.Diagnostics;

namespace RegistryConnector.Worker;

public abstract class NewPoClientWorker : BackgroundService
{
    protected readonly IOptions<ProjectOriginOptions> ProjectOriginOptions;
    protected readonly IOptions<FeatureFlags> FeatureFlags;
    protected readonly ILogger Logger;

    public NewPoClientWorker(
        IOptions<ProjectOriginOptions> projectOriginOptions,
        IOptions<FeatureFlags> featureFlags,
        ILogger logger)
    {
        ProjectOriginOptions = projectOriginOptions;
        FeatureFlags = featureFlags;
        Logger = logger;

        logger.LogInformation("key length: {keyLength}", projectOriginOptions.Value.Dk1IssuerPrivateKeyPem.Length);
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

    protected async Task<WalletSectionReference> CreateWalletSectionReference(string bearerToken)
    {
        Logger.LogInformation("Creating channel for {walletUrl}", ProjectOriginOptions.Value.WalletUrl);
        using var channel = GrpcChannel.ForAddress(ProjectOriginOptions.Value.WalletUrl);

        var walletServiceClient = new WalletService.WalletServiceClient(channel);
        var createWalletSectionRequest = new CreateWalletSectionRequest();
        var headers = new Metadata
            { { "Authorization", $"Bearer {bearerToken}" } };
        var walletSectionReference =
            await walletServiceClient.CreateWalletSectionAsync(createWalletSectionRequest, headers);

        Logger.LogInformation("Received {response}", walletSectionReference);
        return walletSectionReference;
    }

    protected async Task SendSliceToWallet(WalletSectionReference walletSectionReference, IssuedEvent issuedEvent, SecretCommitmentInfo secretCommitmentInfo, uint position)
    {
        Logger.LogInformation("Creating channel for {walletUrl}", ProjectOriginOptions.Value.WalletUrl);
        using var channel = GrpcChannel.ForAddress(ProjectOriginOptions.Value.WalletUrl);

        var receiveSliceServiceClient = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

        var certificateId = new ProjectOrigin.Common.V1.FederatedStreamId
        {
            Registry = issuedEvent.CertificateId.Registry, StreamId = new ProjectOrigin.Common.V1.Uuid { Value = issuedEvent.CertificateId.StreamId.Value }
        };

        var receiveRequest = new ReceiveRequest
        {
            CertificateId = certificateId,
            Quantity = secretCommitmentInfo.Message,
            RandomR = ByteString.CopyFrom(secretCommitmentInfo.BlindingValue),
            WalletSectionPublicKey = walletSectionReference.SectionPublicKey,
            WalletSectionPosition = position
        };
        var receiveResponse = await receiveSliceServiceClient.ReceiveSliceAsync(receiveRequest);

        Logger.LogInformation("Received {response}", receiveResponse);
    }

    protected async Task<IEnumerable<GranularCertificate>> GetCertificatesFromWallet(string bearerToken, int queryForSeconds)
    {
        Logger.LogInformation("Creating channel for {walletUrl}", ProjectOriginOptions.Value.WalletUrl);
        using var channel = GrpcChannel.ForAddress(ProjectOriginOptions.Value.WalletUrl);

        var walletServiceClient = new WalletService.WalletServiceClient(channel);
        var headers = new Metadata { { "Authorization", $"Bearer {bearerToken}" } };

        var sw = new Stopwatch();
        sw.Start();
        QueryResponse certificates;
        do
        {
            certificates = await walletServiceClient.QueryGranularCertificatesAsync(new QueryRequest(), headers);
            if (certificates.GranularCertificates.Count == 0)
                await Task.Delay(1000);
        } while (certificates.GranularCertificates.Count == 0 && sw.Elapsed < TimeSpan.FromSeconds(queryForSeconds));

        return certificates.GranularCertificates;
    }
}
