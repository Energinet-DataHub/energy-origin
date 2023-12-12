using System;
using System.Threading.Tasks;
using DataContext;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.V1;

namespace RegistryConnector.Worker.RoutingSlips;

public record IssueToRegistryArguments(Transaction Transaction, Guid CertificateId);

public class IssueToRegistryActivity : IExecuteActivity<IssueToRegistryArguments>
{
    private readonly RegistryService.RegistryServiceClient client;
    private readonly ILogger<IssueToRegistryActivity> logger;

    public IssueToRegistryActivity(RegistryService.RegistryServiceClient client, ILogger<IssueToRegistryActivity> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<IssueToRegistryArguments> context)
    {
        logger.LogInformation("Issuing to Registry for certificate id {certificateId}. TrackingNumber: {trackingNumber}",
            context.Arguments.CertificateId, context.TrackingNumber);

        // The Registry is idempotent wrt. sending the same Transaction. Re-sending the transaction
        // after it has been committed, will not change the status of the initial transaction

        var request = new SendTransactionsRequest();
        request.Transactions.Add(context.Arguments.Transaction);

        await client.SendTransactionsAsync(request);

        return context.Completed();
    }
}

public class IssueToRegistryActivityDefinition : ExecuteActivityDefinition<IssueToRegistryActivity, IssueToRegistryArguments>
{
    private readonly IServiceProvider provider;
    private readonly RetryOptions retryOptions;
    public IssueToRegistryActivityDefinition(IOptions<RetryOptions> options, IServiceProvider provider)
    {
        this.provider = provider;
        retryOptions = options.Value;
    }

    protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<IssueToRegistryActivity, IssueToRegistryArguments> executeActivityConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(retryOptions.IssueToRegistryActivityRetryCount, TimeSpan.FromMilliseconds(500)));
        endpointConfigurator.UseDelayedRedelivery(r => r.Intervals(TimeSpan.FromDays(1), TimeSpan.FromDays(1), TimeSpan.FromDays(1), TimeSpan.FromDays(1)));

        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(provider);
    }
}
