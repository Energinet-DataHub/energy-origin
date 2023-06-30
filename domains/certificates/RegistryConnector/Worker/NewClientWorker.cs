using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSec.Cryptography;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Register.V1;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.WalletSystem.V1;
using Commitment = ProjectOrigin.Electricity.V1.Commitment;

namespace RegistryConnector.Worker;

public class NewClientWorker : BackgroundService
{
    private readonly IOptions<ProjectOriginOptions> projectOriginOptions;
    private readonly ILogger<NewClientWorker> logger;
    private readonly Key dk1IssuerKey;

    public NewClientWorker(
        IOptions<ProjectOriginOptions> projectOriginOptions,
        ILogger<NewClientWorker> logger)
    {
        this.projectOriginOptions = projectOriginOptions;
        this.logger = logger;

        dk1IssuerKey = projectOriginOptions.Value.Dk1IssuerPrivateKeyPem.Any()
            ? Key.Import(SignatureAlgorithm.Ed25519, projectOriginOptions.Value.Dk1IssuerPrivateKeyPem, KeyBlobFormat.PkixPrivateKey) //TODO: Can this be done differently using ProjectOrigin.HierarchicalDeterministicKeys?
            : Key.Create(SignatureAlgorithm.Ed25519);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var walletSectionReference = await CreateWalletSectionReference();

            var ownerPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(walletSectionReference.SectionPublicKey.Span);

            var commitment = new SecretCommitmentInfo(42);
            var issuedEvent = await IssueCertificateInRegistry(commitment, ownerPublicKey.GetPublicKey());

            await SendSliceToWallet(walletSectionReference, issuedEvent, commitment);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Bad");
            logger.LogWarning(ex.Message);
        }
    }

    private async Task<WalletSectionReference> CreateWalletSectionReference()
    {
        using var channel = GrpcChannel.ForAddress(projectOriginOptions.Value.WalletUrl);

        var walletServiceClient = new WalletService.WalletServiceClient(channel);
        var createWalletSectionRequest = new CreateWalletSectionRequest();
        var headers = new Metadata
            { { "Authorization", $"Bearer {GenerateToken("issuer", "aud", Guid.NewGuid().ToString(), "foo")}" } };
        var walletSectionReference =
            await walletServiceClient.CreateWalletSectionAsync(createWalletSectionRequest, headers);

        logger.LogInformation("Received {response}", walletSectionReference);
        return walletSectionReference;
    }

    private async Task<IssuedEvent> IssueCertificateInRegistry(SecretCommitmentInfo commitment, IPublicKey ownerKey)
    {
        var id = new ProjectOrigin.Common.V1.FederatedStreamId
        {
            Registry = projectOriginOptions.Value.RegistryName,
            StreamId = new ProjectOrigin.Common.V1.Uuid { Value = Guid.NewGuid().ToString() }
        };

        var issuedEvent = new IssuedEvent
        {
            CertificateId = id,
            Type = ProjectOrigin.Electricity.V1.GranularCertificateType.Production,
            Period = new DateInterval { Start = Timestamp.FromDateTimeOffset(DateTimeOffset.Now), End = Timestamp.FromDateTimeOffset(DateTimeOffset.Now.AddHours(1)) },
            GridArea = "DK1",
            QuantityCommitment = new Commitment
            {
                Content = ByteString.CopyFrom(commitment.Commitment.C),
                RangeProof = ByteString.CopyFrom(commitment.CreateRangeProof(id.StreamId.Value))
            },
            OwnerPublicKey = new ProjectOrigin.Electricity.V1.PublicKey
            {
                Content = ByteString.CopyFrom(ownerKey.Export()),
                Type = KeyType.Secp256K1
            }
        };

        using var channel = GrpcChannel.ForAddress(projectOriginOptions.Value.RegistryUrl);
        var client = new RegistryService.RegistryServiceClient(channel);

        var header = new TransactionHeader
        {
            FederatedStreamId = id,
            PayloadType = IssuedEvent.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(issuedEvent.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };
        var headerSignature = SignatureAlgorithm.Ed25519.Sign(dk1IssuerKey, header.ToByteArray());
        var transactions = new Transaction
        {
            Header = header,
            HeaderSignature = ByteString.CopyFrom(headerSignature),
            Payload = issuedEvent.ToByteString()
        };

        var request = new SendTransactionsRequest();
        request.Transactions.Add(transactions);

        await client.SendTransactionsAsync(request);

        var statusRequest = new GetTransactionStatusRequest
        {
            Id = Convert.ToBase64String(SHA256.HashData(transactions.ToByteArray()))
        };

        var began = DateTime.Now;
        while (true)
        {
            var status = await client.GetTransactionStatusAsync(statusRequest);

            if (status.Status == TransactionState.Committed)
                break;
            else if (status.Status == TransactionState.Failed)
                throw new Exception("Failed to issue certificate");
            else
                await Task.Delay(1000);

            if (DateTime.Now - began > TimeSpan.FromMinutes(1))
                throw new Exception("Timed out waiting for transaction to commit");
        }

        return issuedEvent;
    }

    private async Task SendSliceToWallet(WalletSectionReference walletSectionReference, IssuedEvent issuedEvent, SecretCommitmentInfo secretCommitmentInfo)
    {
        using var channel = GrpcChannel.ForAddress(projectOriginOptions.Value.WalletUrl);

        var receiveSliceServiceClient = new ReceiveSliceService.ReceiveSliceServiceClient(channel);
        
        var certificateId = new FederatedStreamId
        {
            Registry = issuedEvent.CertificateId.Registry, StreamId = new ProjectOrigin.Register.V1.Uuid { Value = issuedEvent.CertificateId.StreamId.Value }
        };
        
        var receiveRequest = new ReceiveRequest
        {
            CertificateId =  certificateId,
            Quantity = secretCommitmentInfo.Message,
            RandomR = ByteString.CopyFrom(secretCommitmentInfo.BlindingValue),
            WalletSectionPublicKey = walletSectionReference.SectionPublicKey,
            WalletSectionPosition = 1
        };
        var receiveResponse = await receiveSliceServiceClient.ReceiveSliceAsync(receiveRequest);

        logger.LogInformation("Received {response}", receiveResponse);
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
}
