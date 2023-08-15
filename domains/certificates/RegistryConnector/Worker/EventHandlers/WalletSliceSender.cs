using System.Threading.Tasks;
using Contracts.Certificates;
using Google.Protobuf;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;

namespace RegistryConnector.Worker.EventHandlers;

public class WalletSliceSender : IConsumer<CertificateIssuedInRegistryEvent>
{
    private readonly ILogger<WalletSliceSender> logger;
    private readonly ProjectOriginOptions projectOriginOptions;

    public WalletSliceSender(IOptions<ProjectOriginOptions> projectOriginOptions, ILogger<WalletSliceSender> logger)
    {
        this.logger = logger;
        this.projectOriginOptions = projectOriginOptions.Value;
    }

    public async Task Consume(ConsumeContext<CertificateIssuedInRegistryEvent> context)
    {
        var message = context.Message;

        using var channel = GrpcChannel.ForAddress(message.WalletUrl);
        var client = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

        var receiveRequest = new ReceiveRequest
        {
            CertificateId = new FederatedStreamId
            {
                Registry = projectOriginOptions.RegistryName, //TODO: Should this be from the message?
                StreamId = new Uuid { Value = message.CertificateId.ToString() }
            },
            Quantity = (uint)message.Quantity, //TODO: uint/long
            RandomR = ByteString.CopyFrom(message.BlindingValue),
            WalletDepositEndpointPublicKey = ByteString.CopyFrom(message.WalletPublicKey),
            WalletDepositEndpointPosition = 42 //TODO: Calculate
        };

        var _ = await client.ReceiveSliceAsync(receiveRequest);

        logger.LogInformation("Certificate {id} sent to wallet", message.CertificateId);
    }
}
