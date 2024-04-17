using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataContext;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOriginClients;

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
        logger.LogInformation("Sending slice to Wallet with url {WalletUrl} for certificate id {certificateId}. TrackingNumber: {trackingNumber}",
            context.Arguments.WalletUrl, context.Arguments.ReceiveRequest.CertificateId.StreamId, context.TrackingNumber);

        var client = new HttpClient();
        var requestStr = JsonSerializer.Serialize(context.Arguments.ReceiveRequest);
        var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

        // The Wallet is idempotent wrt. sending the same ReceiveRequest.
        var res = await client.PostAsync(context.Arguments.WalletUrl, content);
        res.EnsureSuccessStatusCode();

        logger.LogInformation("Slice sent to Wallet for certificate id {certificateId}. TrackingNumber: {trackingNumber}",
            context.Arguments.ReceiveRequest.CertificateId.StreamId, context.TrackingNumber);

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

        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    }
}
