using System;
using System.Threading.Tasks;
using DataContext;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

public class SendToWalletActivityDefinition : ExecuteActivityDefinition<SendToWalletActivity, SendToWalletArguments>
{
    private readonly RetryOptions retryOptions;

    public SendToWalletActivityDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureExecuteActivity(
        IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<SendToWalletActivity, SendToWalletArguments> executeActivityConfigurator,
        IRegistrationContext context
        )
    {
        //endpointConfigurator.UseDelayedRedelivery(r => r.Interval(retryOptions.DefaultSecondLevelRetryCount, TimeSpan.FromDays(1)));

        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));

        endpointConfigurator.UseEntityFrameworkOutbox<CertificateDbContext>(context);
    }
}
