using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Contracts.Certificates;
using Google.Protobuf;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using Attribute = ProjectOrigin.Electricity.V1.Attribute;
using PublicKey = ProjectOrigin.Electricity.V1.PublicKey;

namespace RegistryConnector.Worker.EventHandlers;

public class ProductionCertificateCreatedEventHandler : IConsumer<ProductionCertificateCreatedEvent> //TODO: Should this be a JobConsumer?
{
    private readonly ILogger<ProductionCertificateCreatedEventHandler> logger;
    private readonly ProjectOriginOptions projectOriginOptions;

    public ProductionCertificateCreatedEventHandler(
        IOptions<ProjectOriginOptions> projectOriginOptions,
        ILogger<ProductionCertificateCreatedEventHandler> logger)
    {
        this.projectOriginOptions = projectOriginOptions.Value;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductionCertificateCreatedEvent> context)
    {
        var commitment = new SecretCommitmentInfo((uint)context.Message.ShieldedQuantity.Value); //TODO: commitment should be part of message

        var ownerKey = new Secp256k1Algorithm().GenerateNewPrivateKey(); //TODO: Derive new public key from Deposit Endpoint Reference owner key with calculated position
        var ownerPublicKey = ownerKey.PublicKey;

        var id = new ProjectOrigin.Common.V1.FederatedStreamId
        {
            Registry = projectOriginOptions.RegistryName,
            StreamId = new ProjectOrigin.Common.V1.Uuid { Value = context.Message.CertificateId.ToString() }
        };

        var issuedEvent = new IssuedEvent
        {
            CertificateId = id,
            Type = GranularCertificateType.Production,
            Period = context.Message.Period.ToDateInterval(),
            GridArea = context.Message.GridArea,
            QuantityCommitment = new ProjectOrigin.Electricity.V1.Commitment
            {
                Content = ByteString.CopyFrom(commitment.Commitment.C),
                RangeProof = ByteString.CopyFrom(commitment.CreateRangeProof(id.StreamId.Value))
            },
            OwnerPublicKey = new PublicKey
            {
                Content = ByteString.CopyFrom(ownerPublicKey.Export()),
                Type = KeyType.Secp256K1
            }
        };

        issuedEvent.Attributes.Add(new Attribute
        {
            Key = "AssetId",
            Value = context.Message.ShieldedGsrn.Value.Value
        });

        var header = new TransactionHeader
        {
            FederatedStreamId = id,
            PayloadType = IssuedEvent.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(issuedEvent.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(), // TODO: Can this be used in case we send the same message twice?
        };
        var headerSignature = projectOriginOptions.Dk1IssuerKey.Sign(header.ToByteArray()).ToArray(); //TODO: Use issuer for right gridarea
        var transaction = new Transaction
        {
            Header = header,
            HeaderSignature = ByteString.CopyFrom(headerSignature),
            Payload = issuedEvent.ToByteString()
        };

        var request = new SendTransactionsRequest();
        request.Transactions.Add(transaction);

        using var channel = GrpcChannel.ForAddress(projectOriginOptions.RegistryUrl); //TODO: Is this bad practice? Should the channel be re-used?
        var client = new RegistryService.RegistryServiceClient(channel);

        await client.SendTransactionsAsync(request);

        var statusRequest = new GetTransactionStatusRequest
        {
            Id = Convert.ToBase64String(SHA256.HashData(transaction.ToByteArray()))
        };

        logger.LogInformation("Sending status request {statusRequest}", statusRequest);

        try
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (true)
            {
                var status = await client.GetTransactionStatusAsync(statusRequest);

                if (status.Status == TransactionState.Committed)
                {
                    logger.LogInformation("Certificate {id} issued in registry", context.Message.CertificateId);
                    await context.Publish(new CertificateIssuedInRegistryEvent(context.Message.CertificateId));
                    break;
                }

                if (status.Status == TransactionState.Failed)
                {
                    logger.LogInformation("Certificate {id} rejected by registry", context.Message.CertificateId);
                    await context.Publish(new CertificateRejectedInRegistryEvent(context.Message.CertificateId, status.Message));
                    break;
                }

                await Task.Delay(1000);

                if (stopWatch.Elapsed > TimeSpan.FromMinutes(5))
                    throw new Exception("Timed out waiting for transaction to commit"); //TODO: What to do here...?
            }
        }
        catch (Exception ex)
        {
            logger.LogError("BAD!!!");
        }
    }
}
