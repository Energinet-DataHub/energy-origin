using System.Threading.Tasks;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.WalletSystem.V1;

namespace RegistryConnector.Worker.RoutingSlips;

public record SendToWalletArguments(string WalletUrl, ReceiveRequest ReceiveRequest);

public class SendToWalletActivity : IExecuteActivity<SendToWalletArguments>
{
    private readonly ILogger<SendToWalletActivity> logger;

    public SendToWalletActivity(ILogger<SendToWalletActivity> logger)
    {
        this.logger = logger;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<SendToWalletArguments> context)
    {
        logger.LogInformation("Sending slice to Wallet for certificate id {certificateId}. TrackingNumber: {trackingNumber}",
            context.Arguments.ReceiveRequest.CertificateId.StreamId.Value, context.TrackingNumber);

        using var channel = GrpcChannel.ForAddress(context.Arguments.WalletUrl);
        var client = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

        // The Wallet is idempotent wrt. sending the same ReceiveRequest.

        await client.ReceiveSliceAsync(context.Arguments.ReceiveRequest);

        return context.Completed();
    }
}
