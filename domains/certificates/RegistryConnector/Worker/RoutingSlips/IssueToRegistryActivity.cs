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
    private readonly RetryOptions retryOptions;
    public IssueToRegistryActivityDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureExecuteActivity(
        IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<IssueToRegistryActivity, IssueToRegistryArguments> executeActivityConfigurator,
        IRegistrationContext context
        )
    {
        //endpointConfigurator.UseDelayedRedelivery(r => r.Interval(retryOptions.DefaultSecondLevelRetryCount, TimeSpan.FromSeconds(10)));

        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));

        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    }
}
