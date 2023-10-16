using System;
using System.Threading.Tasks;
using Contracts.Certificates;
using Google.Protobuf;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;

namespace RegistryConnector.Worker.EventHandlers;

public class WalletSliceSender : IConsumer<CertificateIssuedInRegistryEvent>, IConsumer<Contracts.Certificates.CertificateIssuedInRegistry.V1.CertificateIssuedInRegistryEvent>
{
    private readonly ILogger<WalletSliceSender> logger;

    public WalletSliceSender(ILogger<WalletSliceSender> logger) => this.logger = logger;

    //TODO this function will be deleted after the PR has been merged
    public async Task Consume(ConsumeContext<CertificateIssuedInRegistryEvent> context)
    {
        var message = context.Message;

        if (message.Quantity > uint.MaxValue)
            throw new ArgumentOutOfRangeException($"Cannot cast quantity {message.Quantity} to uint");

        using var channel = GrpcChannel.ForAddress(message.WalletUrl);
        var client = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

        var receiveRequest = new ReceiveRequest
        {
            CertificateId = new FederatedStreamId
            {
                Registry = message.RegistryName,
                StreamId = new Uuid { Value = message.CertificateId.ToString() }
            },
            Quantity = (uint)message.Quantity,
            RandomR = ByteString.CopyFrom(message.BlindingValue),
            WalletDepositEndpointPublicKey = ByteString.CopyFrom(message.WalletPublicKey),
            WalletDepositEndpointPosition = message.WalletDepositEndpointPosition
        };

        await client.ReceiveSliceAsync(receiveRequest);

        logger.LogInformation("Certificate {id} sent to wallet", message.CertificateId);
    }

    public async Task Consume(ConsumeContext<Contracts.Certificates.CertificateIssuedInRegistry.V1.CertificateIssuedInRegistryEvent> context)
    {
        var message = context.Message;

        if (message.Quantity > uint.MaxValue)
            throw new ArgumentOutOfRangeException($"Cannot cast quantity {message.Quantity} to uint");

        using var channel = GrpcChannel.ForAddress(message.WalletUrl);
        var client = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

        var receiveRequest = new ReceiveRequest
        {
            CertificateId = new FederatedStreamId
            {
                Registry = message.RegistryName,
                StreamId = new Uuid { Value = message.CertificateId.ToString() }
            },
            Quantity = (uint)message.Quantity,
            RandomR = ByteString.CopyFrom(message.BlindingValue),
            WalletDepositEndpointPublicKey = ByteString.CopyFrom(message.WalletPublicKey),
            WalletDepositEndpointPosition = message.WalletDepositEndpointPosition
        };

        await client.ReceiveSliceAsync(receiveRequest);

        logger.LogInformation("Certificate {id} sent to wallet", message.CertificateId);
    }
}
