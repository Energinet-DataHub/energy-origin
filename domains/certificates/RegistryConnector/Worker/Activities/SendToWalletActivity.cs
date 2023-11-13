using System.Threading.Tasks;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.WalletSystem.V1;

namespace RegistryConnector.Worker.Activities;

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
        logger.LogInformation("Wallet. TrackingNumber: {trackingNumber}. Arguments: {args}", context.TrackingNumber, context.Arguments);

        // TODO: Think this should be handled earlier - in the routing slip builder
        //if (message.Quantity > uint.MaxValue)
        //    throw new ArgumentOutOfRangeException($"Cannot cast quantity {message.Quantity} to uint");

        using var channel = GrpcChannel.ForAddress(context.Arguments.WalletUrl);
        var client = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

        await client.ReceiveSliceAsync(context.Arguments.ReceiveRequest);

        return context.Completed();
    }
}
