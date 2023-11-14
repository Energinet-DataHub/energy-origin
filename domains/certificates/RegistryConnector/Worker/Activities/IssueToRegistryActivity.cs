using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.V1;

namespace RegistryConnector.Worker.Activities;

public record IssueToRegistryArguments(Transaction Transaction);

public class IssueToRegistryActivity : IExecuteActivity<IssueToRegistryArguments>
{
    private readonly ProjectOriginOptions projectOriginOptions;
    private readonly ILogger<IssueToRegistryActivity> logger;

    public IssueToRegistryActivity(IOptions<ProjectOriginOptions> projectOriginOptions, ILogger<IssueToRegistryActivity> logger)
    {
        this.projectOriginOptions = projectOriginOptions.Value;
        this.logger = logger;
    }

    
    public async Task<ExecutionResult> Execute(ExecuteContext<IssueToRegistryArguments> context)
    {
        logger.LogInformation("Registry. TrackingNumber: {trackingNumber}. Arguments: {args}. Retry {r1} {r2} {r3}", context.TrackingNumber, context.Arguments, context.GetRetryAttempt(), context.GetRetryCount(), context.GetRedeliveryCount());

        using var channel = GrpcChannel.ForAddress(projectOriginOptions.RegistryUrl); //TODO: Is this bad practice? Should the channel be re-used?
        var client = new RegistryService.RegistryServiceClient(channel);

        // The Registry is idempotent wrt. the sending the same Transaction. Re-sending the transaction
        // after it has been committed, will not change the status of the initial transaction

        var request = new SendTransactionsRequest();
        request.Transactions.Add(context.Arguments.Transaction);

        await client.SendTransactionsAsync(request);

        return context.Completed();
    }
}

public class IssueToRegistryActivityDefinition : ExecuteActivityDefinition<IssueToRegistryActivity, IssueToRegistryArguments>
{
    protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<IssueToRegistryActivity, IssueToRegistryArguments> executeActivityConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromMilliseconds(500)));
    }
}
