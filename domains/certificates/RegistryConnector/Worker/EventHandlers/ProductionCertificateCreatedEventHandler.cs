using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Contracts.Certificates;
using Google.Protobuf;
using Google.Protobuf.Collections;
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

        //var x = new ProjectOrigin.Electricity.V1.Attribute
        //{
        //    Key = "AssetId",
        //    Value = context.Message.ShieldedGsrn.Value.Value
        //};
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
            },
            //Attributes = new RepeatedField<Attribute> {x}

            //TODO: Set Attributes
        };

        logger.LogInformation("Creating channel for {registryUrl}", projectOriginOptions.RegistryUrl);

        using var channel = GrpcChannel.ForAddress(projectOriginOptions.RegistryUrl);
        var client = new RegistryService.RegistryServiceClient(channel);

        var header = new TransactionHeader
        {
            FederatedStreamId = id,
            PayloadType = IssuedEvent.Descriptor.FullName,
            PayloadSha512 = ByteString.CopyFrom(SHA512.HashData(issuedEvent.ToByteArray())),
            Nonce = Guid.NewGuid().ToString(),
        };
        var headerSignature = projectOriginOptions.Dk1IssuerKey.Sign(header.ToByteArray()).ToArray();
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
            {
                await context.Publish(new CertificateIssuedInRegistryEvent(context.Message.CertificateId));
                break;
            }

            if (status.Status == TransactionState.Failed)
            {
                await context.Publish(new CertificateRejectedInRegistryEvent(context.Message.CertificateId, status.Message));
                break;
            }

            await Task.Delay(1000);

            if (DateTime.Now - began > TimeSpan.FromMinutes(1))
                throw new Exception("Timed out waiting for transaction to commit");
        }

        //var msg = context.Message;
        //var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        //var commandBuilder = new ElectricityCommandBuilder();

        ////TODO which PO registry should the certificate be issued to? See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1520
        //var federatedCertificateId = new FederatedCertifcateId(
        //    registryOptions.RegistryName,
        //    msg.CertificateId);

        ////TODO GSRN in Project Origin is a ulong. Should be a string? See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1519
        //commandBuilder.IssueProductionCertificate(
        //    id: federatedCertificateId,
        //    inteval: msg.Period.ToDateInterval(),
        //    gridArea: msg.GridArea,
        //    gsrn: ulong.Parse(msg.ShieldedGsrn.Value.Value),
        //    quantity: new ShieldedValue((uint)msg.ShieldedQuantity.Value),
        //    owner: ownerKey.PublicKey,
        //    issuingBodySigner: issuerKey.Value,
        //    fuelCode: msg.Technology.FuelCode,
        //    techCode: msg.Technology.TechCode
        //);

        ////TODO sometimes Execute returns faster than the wrapped msg is saved to cache. See issue: https://github.com/project-origin/registry/issues/61
        //var commandId = await commandBuilder.Execute(registerClient);

        //var wrappedMsg = new MessageWrapper<ProductionCertificateCreatedEvent>(context);

        //cache.AddCertificateWithCommandId(commandId, wrappedMsg);

        //logger.LogInformation("Sent command. Id={id}", commandId.ToHex());
    }
}
