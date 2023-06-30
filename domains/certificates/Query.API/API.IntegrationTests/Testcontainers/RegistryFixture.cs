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
    private const string RegistryImage = "ghcr.io/project-origin/registry-server:0.2.0-rc.17";
    private const string ElectricityVerifierImage = "ghcr.io/project-origin/electricity-server:0.2.0-rc.17";
    private const int GrpcPort = 80;
    private const string Area = "Narnia";
    private const string RegistryName = "TestRegistry";

    private Lazy<IContainer> _registryContainer;
    private IContainer _verifierContainer;

    public string IssuerArea => Area;
    public string Name => RegistryName;
    public IPrivateKey IssuerKey { get; init; }


    public string RegistryUrl => $"http://{_registryContainer.Value.Hostname}:{_registryContainer.Value.GetMappedPublicPort(GrpcPort)}";

    public RegistryFixture()
    {
        IssuerKey = Algorithms.Ed25519.GenerateNewPrivateKey();

        _verifierContainer = new ContainerBuilder()
                .WithImage(ElectricityVerifierImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment($"Issuers__{IssuerArea}", Convert.ToBase64String(Encoding.UTF8.GetBytes(IssuerKey.PublicKey.ExportPkixText())))
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
                .Build();

        _registryContainer = new Lazy<IContainer>(() =>
        {
            var verifierUrl = $"http://{_verifierContainer.IpAddress}:{GrpcPort}";
            return new ContainerBuilder()
                .WithImage(RegistryImage)
                .WithPortBinding(GrpcPort, true)
                .WithEnvironment($"Verifiers__project_origin.electricity.v1", verifierUrl)
                .WithEnvironment($"RegistryName", RegistryName)
                .WithEnvironment($"IMMUTABLELOG__TYPE", "log")
                .WithEnvironment($"VERIFIABLEEVENTSTORE__BATCHSIZEEXPONENT", "0")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(GrpcPort)
                    )
                .Build();
        });
    }

    public async Task InitializeAsync()
    {
        await _verifierContainer.StartAsync()
            .ConfigureAwait(false);

        await _registryContainer.Value.StartAsync()
            .ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_registryContainer.IsValueCreated)
            await _registryContainer.Value.StopAsync();
        await _verifierContainer.StopAsync();
    }

    public async Task<IssuedEvent> IssueCertificate(GranularCertificateType type, ProjectOrigin.PedersenCommitment.SecretCommitmentInfo commitment, IPublicKey ownerKey)
    {
        var id = new FederatedStreamId()
        {
            Registry = RegistryName,
            StreamId = new Uuid { Value = Guid.NewGuid().ToString() }
        };

        var issuedEvent = new IssuedEvent
        {
            CertificateId = id,
            Type = type,
            Period = new DateInterval { Start = Timestamp.FromDateTimeOffset(DateTimeOffset.Now), End = Timestamp.FromDateTimeOffset(DateTimeOffset.Now.AddHours(1)) },
            GridArea = Area,
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
