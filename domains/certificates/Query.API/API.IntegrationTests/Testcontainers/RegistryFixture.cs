extern alias registryConnector;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using registryConnector::ProjectOrigin.Common.V1;
using registryConnector::ProjectOrigin.Electricity.V1;
using registryConnector::ProjectOrigin.Registry.V1;
using Xunit;

namespace API.IntegrationTests.Testcontainers;

public class RegistryFixture : IAsyncLifetime
{
    private const string registryImage = "ghcr.io/project-origin/registry-server:0.2.0-rc.17";
    private const string electricityVerifierImage = "ghcr.io/project-origin/electricity-server:0.2.0-rc.17";
    private const int grpcPort = 80;
    private const string area = "Narnia";
    private const string registryName = "TestRegistry";

    private readonly Lazy<IContainer> registryContainer;
    private readonly IContainer verifierContainer;

    public string IssuerArea => area;
    public string Name => registryName;
    public IPrivateKey IssuerKey { get; init; }


    public string RegistryUrl => $"http://{registryContainer.Value.Hostname}:{registryContainer.Value.GetMappedPublicPort(grpcPort)}";

    public RegistryFixture()
    {
        IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        verifierContainer = new ContainerBuilder()
                .WithImage(electricityVerifierImage)
                .WithPortBinding(grpcPort, true)
                .WithEnvironment($"Issuers__{IssuerArea}", Convert.ToBase64String(Encoding.UTF8.GetBytes(IssuerKey.PublicKey.ExportPkixText())))
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(grpcPort)
                    )
                .Build();

        registryContainer = new Lazy<IContainer>(() =>
        {
            var verifierUrl = $"http://{verifierContainer.IpAddress}:{grpcPort}";
            return new ContainerBuilder()
                .WithImage(registryImage)
                .WithPortBinding(grpcPort, true)
                .WithEnvironment($"Verifiers__project_origin.electricity.v1", verifierUrl)
                .WithEnvironment($"RegistryName", registryName)
                .WithEnvironment($"IMMUTABLELOG__TYPE", "log")
                .WithEnvironment($"VERIFIABLEEVENTSTORE__BATCHSIZEEXPONENT", "0")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(grpcPort)
                    )
                .Build();
        });
    }

    public async Task InitializeAsync()
    {
        await verifierContainer.StartAsync()
            .ConfigureAwait(false);

        await registryContainer.Value.StartAsync()
            .ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (registryContainer.IsValueCreated)
            await registryContainer.Value.StopAsync();
        await verifierContainer.StopAsync();
    }

    public async Task<IssuedEvent> IssueCertificate(GranularCertificateType type, ProjectOrigin.PedersenCommitment.SecretCommitmentInfo commitment, IPublicKey ownerKey)
    {
        var id = new FederatedStreamId()
        {
            Registry = registryName,
            StreamId = new Uuid { Value = Guid.NewGuid().ToString() }
        };

        var issuedEvent = new IssuedEvent
        {
            CertificateId = id,
            Type = type,
            Period = new DateInterval { Start = Timestamp.FromDateTimeOffset(DateTimeOffset.Now), End = Timestamp.FromDateTimeOffset(DateTimeOffset.Now.AddHours(1)) },
            GridArea = area,
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

        var channel = GrpcChannel.ForAddress(RegistryUrl);
        var client = new RegistryService.RegistryServiceClient(channel);

        var header = new TransactionHeader
        {
            FederatedStreamId = id,
            PayloadType = IssuedEvent.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(issuedEvent.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };
        var transactions = new Transaction
        {
            Header = header,
            HeaderSignature = ByteString.CopyFrom(IssuerKey.Sign(header.ToByteArray())),
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
}
