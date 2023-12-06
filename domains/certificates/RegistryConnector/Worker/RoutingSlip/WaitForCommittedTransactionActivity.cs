using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.V1;

namespace RegistryConnector.Worker.RoutingSlip;

public record WaitForCommittedTransactionArguments(string ShaId);

public class WaitForCommittedTransactionActivity : IExecuteActivity<WaitForCommittedTransactionArguments>
{
    private readonly ILogger<WaitForCommittedTransactionActivity> logger;
    private readonly ProjectOriginOptions projectOriginOptions;

    public WaitForCommittedTransactionActivity(IOptions<ProjectOriginOptions> projectOriginOptions, ILogger<WaitForCommittedTransactionActivity> logger)
    {
        this.logger = logger;
        this.projectOriginOptions = projectOriginOptions.Value;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<WaitForCommittedTransactionArguments> context)
    {
        using var channel = GrpcChannel.ForAddress(projectOriginOptions.RegistryUrl); //TODO: Is this bad practice? Should the channel be re-used?
        var client = new RegistryService.RegistryServiceClient(channel);
        var statusRequest = new GetTransactionStatusRequest { Id = context.Arguments.ShaId };

        try
        {
            var status = await client.GetTransactionStatusAsync(statusRequest);

            if (status.Status == TransactionState.Committed)
            {
                //TODO: Log
                //logger.LogInformation("Certificate {id} issued in registry", certificateId);
                return context.Completed();
            }

            if (status.Status == TransactionState.Failed)
            {
                //logger.LogInformation("Certificate {id} rejected by registry", certificateId);
                return context.Terminate(new List<KeyValuePair<string, object>>
                {
                    new("Reason", "Transaction failed in Registry")
                });
            }

            const string message = "Transaction is still processing on registry.";
            logger.LogDebug(message);
            return context.Faulted(new RegistryTransactionStillProcessingException(message));
        }
        catch (RpcException ex)
        {
            var transientException = new TransientException("Registry communication error", ex);
            logger.LogWarning(transientException, null);
            return context.Faulted(transientException);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get status from registry");
            return context.Faulted(ex);
        }
    }
}

public class WaitForCommittedTransactionActivityDefinition : ExecuteActivityDefinition<WaitForCommittedTransactionActivity, WaitForCommittedTransactionArguments>
{
    protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<WaitForCommittedTransactionActivity, WaitForCommittedTransactionArguments> executeActivityConfigurator)
    {
        //endpointConfigurator.UseDelayedRedelivery(r => r.Incremental());
        //TODO: Configurable values for the retries, which can be overwritten in the integration tests
        endpointConfigurator.UseMessageRetry(r => r
            .Interval(100, TimeSpan.FromSeconds(1))
            .Handle(typeof(TransientException), typeof(RegistryTransactionStillProcessingException)));
        //TODO: Define what to do for e.g. other exceptions. Should they be retried - or will they be redelivered?
    }
}

[Serializable]
public class RegistryTransactionStillProcessingException : Exception
{
    public RegistryTransactionStillProcessingException(string message) : base(message)
    {
    }

    protected RegistryTransactionStillProcessingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}


[Serializable]
public class TransientException : Exception
{
    public TransientException(string message, Exception ex) : base(message, ex)
    {
    }

    protected TransientException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
