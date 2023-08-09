using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using Commitment = ProjectOrigin.Electricity.V1.Commitment;

namespace RegistryConnector.Worker;

public class NewClientWorker : NewPoClientWorker
{
    public NewClientWorker(
        IOptions<ProjectOriginOptions> projectOriginOptions,
        IOptions<FeatureFlags> featureFlags,
        ILogger<NewClientWorker> logger) : base(projectOriginOptions, featureFlags, logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var isEnabled = FeatureFlags.Value.RunsProjectOriginIntegrationSample ? "enabled" : "disabled";
        Logger.LogInformation("NewClientWorker is {isEnabled}", isEnabled);
        if (!FeatureFlags.Value.RunsProjectOriginIntegrationSample)
            return;

        try
        {
            var bearerToken = GenerateToken("issuer", "aud", Guid.NewGuid().ToString(), "foo");
            var walletDepositEndpoint = await CreateWalletDepositEndpoint(bearerToken);

            var ownerPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(walletDepositEndpoint.PublicKey.Span);
            const int position = 42;

            var commitment = new SecretCommitmentInfo(42);
            var issuedEvent = await IssueCertificateInRegistry(commitment, ownerPublicKey.Derive(position).GetPublicKey());

            await SendSliceToWallet(walletDepositEndpoint, issuedEvent, commitment, position);

            var certificates = await GetCertificatesFromWallet(bearerToken, 10);

            Logger.LogInformation("Has {certificates}", certificates);
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Bad");
            Logger.LogWarning(ex.Message);
        }
    }

    private async Task<IssuedEvent> IssueCertificateInRegistry(SecretCommitmentInfo commitment, IPublicKey ownerKey)
    {
        var id = new ProjectOrigin.Common.V1.FederatedStreamId
        {
            Registry = ProjectOriginOptions.Value.RegistryName,
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
            OwnerPublicKey = new PublicKey
            {
                Content = ByteString.CopyFrom(ownerKey.Export()),
                Type = KeyType.Secp256K1
            }
        };

        Logger.LogInformation("Creating channel for {registryUrl}", ProjectOriginOptions.Value.RegistryUrl);
        using var channel = GrpcChannel.ForAddress(ProjectOriginOptions.Value.RegistryUrl);
        var client = new RegistryService.RegistryServiceClient(channel);

        var header = new TransactionHeader
        {
            FederatedStreamId = id,
            PayloadType = IssuedEvent.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(issuedEvent.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };
        var headerSignature = ProjectOriginOptions.Value.GetIssuerKey("DK1").Sign(header.ToByteArray()).ToArray();
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
                throw new Exception($"Failed to issue certificate. Message: {status.Message}");
            else
                await Task.Delay(1000);

            if (DateTime.Now - began > TimeSpan.FromMinutes(1))
                throw new Exception("Timed out waiting for transaction to commit");
        }

        return issuedEvent;
    }
}
