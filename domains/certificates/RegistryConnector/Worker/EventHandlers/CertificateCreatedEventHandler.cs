using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.ValueObjects;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using ProjectOriginClients;

namespace RegistryConnector.Worker.EventHandlers;

public record CertificateCreatedEvent
{
    public required Guid CertificateId { get; init; }
    public required MeteringPointType MeteringPointType { get; init; }
    public required uint Quantity { get; init; }
    public required Period Period { get; init; }
    public required string GridArea { get; init; }
    public required byte[] WalletPublicKey { get; init; }
    public required uint WalletDepositEndpointPosition { get; init; }
    public required Gsrn Gsrn { get; init; }
    public required Technology? Technology { get; init; }
    public required string WalletUrl { get; init; }
};

public class CertificateCreatedEventHandler : IConsumer<CertificateCreatedEvent>
{
    private readonly RegistryService.RegistryServiceClient client;
    private readonly ILogger<CertificateCreatedEventHandler> logger;
    private readonly IKeyGenerator keyGenerator;
    private readonly ProjectOriginRegistryOptions projectOriginRegistryOptions;

    public CertificateCreatedEventHandler(RegistryService.RegistryServiceClient client,
        ILogger<CertificateCreatedEventHandler> logger,
        IKeyGenerator keyGenerator,
        IOptions<ProjectOriginRegistryOptions> projectOriginRegistryOptions)
    {
        this.client = client;
        this.logger = logger;
        this.keyGenerator = keyGenerator;
        this.projectOriginRegistryOptions = projectOriginRegistryOptions.Value;
    }

    public async Task Consume(ConsumeContext<CertificateCreatedEvent> context)
    {
        logger.LogInformation("Issuing to Registry for certificate id {certificateId}.", context.Message.CertificateId);

        var message = context.Message;

        var commitment = new SecretCommitmentInfo(message.Quantity);

        var (ownerPublicKey, issuerKey) = keyGenerator.GenerateKeyInfo(message.Quantity,
            message.WalletPublicKey, message.WalletDepositEndpointPosition, message.GridArea);

        Transaction transaction;
        if (message.MeteringPointType == MeteringPointType.Production)
        {
            var issuedEvent = Registry.CreateIssuedEventForProduction(
                projectOriginRegistryOptions.RegistryName,
                message.CertificateId,
                message.Period.ToDateInterval(),
                message.GridArea,
                message.Gsrn.Value,
                message.Technology!.TechCode,
                message.Technology.FuelCode,
                commitment,
                ownerPublicKey);

            transaction = issuedEvent.CreateTransaction(issuerKey);
        }
        else if (message.MeteringPointType == MeteringPointType.Consumption)
        {
            var issuedEvent = Registry.CreateIssuedEventForConsumption(
                projectOriginRegistryOptions.RegistryName,
                message.CertificateId,
                message.Period.ToDateInterval(),
                message.GridArea,
                message.Gsrn.Value,
                commitment,
                ownerPublicKey);

            transaction = issuedEvent.CreateTransaction(issuerKey);
        }
        else
            throw new CertificateDomainException(string.Format("Unsupported metering point type {0} for certificateId {1}",
                message.MeteringPointType, message.CertificateId));

        var request = new SendTransactionsRequest();
        request.Transactions.Add(transaction);

        await client.SendTransactionsAsync(request);

        await context.Publish(new CertificateSentToRegistryEvent
        {
            CertificateId = message.CertificateId,
            ShaId = transaction.ToShaId(),
            MeteringPointType = message.MeteringPointType,
            Quantity = message.Quantity,
            RandomR = commitment.BlindingValue.ToArray(),
            Registry = projectOriginRegistryOptions.RegistryName,
            WalletEndpointPosition = message.WalletDepositEndpointPosition,
            WalletPublicKey = message.WalletPublicKey,
            WalletUrl = message.WalletUrl
        });
    }
}

public class CertificateCreatedEventHandlerConsumerDefinition : ConsumerDefinition<CertificateCreatedEventHandler>
{
    private readonly RetryOptions retryOptions;

    public CertificateCreatedEventHandlerConsumerDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CertificateCreatedEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3))
            .Handle(typeof(DbUpdateException), typeof(InvalidOperationException)));
    }
}
