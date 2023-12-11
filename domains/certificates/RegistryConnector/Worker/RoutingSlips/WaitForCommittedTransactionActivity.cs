using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Registry.V1;

namespace RegistryConnector.Worker.RoutingSlips;

public record WaitForCommittedTransactionArguments(string ShaId, Guid CertificateId);

public class WaitForCommittedTransactionActivity : IExecuteActivity<WaitForCommittedTransactionArguments>
{
    private readonly RegistryService.RegistryServiceClient client;
    private readonly ILogger<WaitForCommittedTransactionActivity> logger;

    public WaitForCommittedTransactionActivity(RegistryService.RegistryServiceClient client, ILogger<WaitForCommittedTransactionActivity> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<WaitForCommittedTransactionArguments> context)
    {
        var statusRequest = new GetTransactionStatusRequest { Id = context.Arguments.ShaId };

        try
        {
            var status = await client.GetTransactionStatusAsync(statusRequest);

            if (status.Status == TransactionState.Committed)
            {
                logger.LogInformation("Transaction {id} with certificate {certificateId} completed.", context.Arguments.ShaId, context.Arguments.CertificateId);
                return context.Completed();
            }

            if (status.Status == TransactionState.Failed)
            {
                logger.LogWarning("Transaction {id} with certificate {certificateId} failed in registry.", context.Arguments.ShaId, context.Arguments.CertificateId);
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
}


[Serializable]
public class TransientException : Exception
{
    public TransientException(string message, Exception ex) : base(message, ex)
    {
    }
}
