using System;
using System.Threading.Tasks;
using DataContext.ValueObjects;
using Grpc.Core;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.V1;
using RegistryConnector.Worker.Exceptions;

namespace RegistryConnector.Worker.EventHandlers;

public record CertificateSentToRegistryEvent
{
    public required string ShaId { get; init; }
    public required Guid CertificateId { get; init; }
    public required MeteringPointType MeteringPointType { get; init; }
    public required string Registry { get; init; }
    public required uint Quantity { get; init; }
    public required byte[] RandomR { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required byte[] WalletPublicKey { get; init; }
    public required string WalletUrl { get; init; }
}

public class CertificateSentToRegistryEventHandler : IConsumer<CertificateSentToRegistryEvent>
{
    private readonly RegistryService.RegistryServiceClient client;
    private readonly ILogger<CertificateSentToRegistryEventHandler> logger;

    public CertificateSentToRegistryEventHandler(RegistryService.RegistryServiceClient client,
        ILogger<CertificateSentToRegistryEventHandler> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateSentToRegistryEvent> context)
    {
        var message = context.Message;
        var statusRequest = new GetTransactionStatusRequest { Id = message.ShaId };

        try
        {
            var status = await client.GetTransactionStatusAsync(statusRequest);

            if (status.Status == TransactionState.Committed)
            {
                logger.LogInformation("Transaction {id} with certificate {certificateId} completed.", message.ShaId, message.CertificateId);

                await context.Publish<CertificateIssuedInRegistryEvent>(new CertificateIssuedInRegistryEvent
                {
                    CertificateId = message.CertificateId,
                    MeteringPointType = message.MeteringPointType,
                    Quantity = message.Quantity,
                    RandomR = message.RandomR,
                    WalletUrl = message.WalletUrl,
                    WalletEndpointPosition = message.WalletEndpointPosition,
                    WalletPublicKey = message.WalletPublicKey,
                    Registry = message.Registry
                });
                return;
            }

            if (status.Status == TransactionState.Failed)
            {
                logger.LogWarning("Transaction {id} with certificate {certificateId} failed in registry.", message.ShaId, message.CertificateId);

                await context.Publish<CertificateFailedInRegistryEvent>(new CertificateFailedInRegistryEvent
                {
                    MeteringPointType = message.MeteringPointType,
                    CertificateId = message.CertificateId,
                    RejectReason = "Rejected by the registry"
                });
                return;
            }

            string infoMessage = $"Transaction {message.ShaId} is still processing on registry for certificateId: {message.CertificateId}.";
            logger.LogInformation(infoMessage);
            throw new RegistryTransactionStillProcessingException(infoMessage);
        }
        catch (RpcException ex)
        {
            logger.LogWarning("Registry communication error. Exception: {ex}", ex);
            throw new TransientException("Registry communication error");
        }
    }
}

public class CertificateSentToRegistryEventHandlerConsumerDefinition : ConsumerDefinition<CertificateSentToRegistryEventHandler>
{
    private readonly RetryOptions retryOptions;

    public CertificateSentToRegistryEventHandlerConsumerDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CertificateSentToRegistryEventHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Interval(retryOptions.RegistryTransactionStillProcessingRetryCount, TimeSpan.FromSeconds(1))
            .Handle(typeof(RegistryTransactionStillProcessingException)));

        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3))
            .Ignore(typeof(RegistryTransactionStillProcessingException)));
    }
}

[Serializable]
public class RegistryTransactionStillProcessingException : Exception
{
    public RegistryTransactionStillProcessingException(string message) : base(message)
    {
    }
}
