using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataContext;
using Grpc.Core;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.V1;
using RegistryConnector.Worker.Exceptions;

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
                logger.LogWarning("Transaction {id} with certificate {certificateId} failed in registry. Reason: {reason}", context.Arguments.ShaId, context.Arguments.CertificateId, status.Message);
                return context.Terminate(new List<KeyValuePair<string, object>>
                {
                    new("Reason", "Transaction failed in Registry. Reason: " + status.Message)
                });
            }

            string message = $"Transaction {context.Arguments.ShaId} is still processing on registry for certificateId: {context.Arguments.CertificateId}.";
            logger.LogDebug(message);
            return context.Faulted(new RegistryTransactionStillProcessingException(message));
        }
        catch (RpcException ex)
        {
            var transientException = new TransientException("Registry communication error");
            logger.LogWarning("Registry communication error. Exception: {ex}", ex);
            return context.Faulted(transientException);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get status from registry");
            return context.Faulted(ex);
        }
    }
}

public class WaitForCommittedTransactionActivityDefinition : ExecuteActivityDefinition<WaitForCommittedTransactionActivity, WaitForCommittedTransactionArguments>
{
    private readonly RetryOptions retryOptions;

    public WaitForCommittedTransactionActivityDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureExecuteActivity(
        IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<WaitForCommittedTransactionActivity, WaitForCommittedTransactionArguments> executeActivityConfigurator,
        IRegistrationContext context
        )
    {
        //endpointConfigurator.UseDelayedRedelivery(r => r.Interval(retryOptions.DefaultSecondLevelRetryCount, TimeSpan.FromDays(1)));

        endpointConfigurator.UseMessageRetry(r => r
            .Interval(retryOptions.RegistryTransactionStillProcessingRetryCount, TimeSpan.FromSeconds(1))
            .Handle(typeof(TransientException), typeof(RegistryTransactionStillProcessingException)));

        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3))
            .Ignore(typeof(TransientException), typeof(RegistryTransactionStillProcessingException)));

        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    }
}

[Serializable]
public class RegistryTransactionStillProcessingException : Exception
{
    public RegistryTransactionStillProcessingException(string message) : base(message)
    {
    }
}

