using MassTransit;
using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataContext.Models;
using Microsoft.Extensions.Options;

namespace RegistryConnector.Worker.EventHandlers;

public record CertificateIssuedInRegistryEvent
{
    public required Guid CertificateId { get; init; }
    public required MeteringPointType MeteringPointType { get; init; }
    public required string Registry { get; init; }
    public required uint Quantity { get; init; }
    public required byte[] RandomR { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required byte[] WalletPublicKey { get; init; }
    public required string WalletUrl { get; init; }
}

public class CertificateIssuedInRegistryEventHandler : IConsumer<CertificateIssuedInRegistryEvent>
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly ILogger<CertificateIssuedInRegistryEventHandler> logger;

    public CertificateIssuedInRegistryEventHandler(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger<CertificateIssuedInRegistryEventHandler> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateIssuedInRegistryEvent> context)
    {
        var message = context.Message;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        Certificate? certificate = message.MeteringPointType == MeteringPointType.Production
            ? await dbContext.ProductionCertificates.FindAsync([message.CertificateId],
                context.CancellationToken)
            : await dbContext.ConsumptionCertificates.FindAsync([message.CertificateId],
                context.CancellationToken);

        if (certificate == null)
        {
            logger.LogWarning("Certificate with certificateId {message.CertificateId} and meteringPointType {message.MeteringPointType} not found.", message.CertificateId, message.MeteringPointType);
            return;
        }

        if (!certificate.IsIssued)
            certificate.Issue();

        await context.Publish<CertificateMarkedAsIssuedEvent>(new CertificateMarkedAsIssuedEvent
        {
            CertificateId = message.CertificateId,
            Registry = message.Registry,
            Quantity = message.Quantity,
            RandomR = message.RandomR,
            WalletEndpointPosition = message.WalletEndpointPosition,
            WalletPublicKey = message.WalletPublicKey,
            WalletUrl = message.WalletUrl
        });
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Certificate with certificateId {message.CertificateId} and meteringPointType {message.MeteringPointType} issued.", message.CertificateId, message.MeteringPointType);
    }
}

public class CertificateIssuedInRegistryEventHandlerConsumerDefinition : ConsumerDefinition<CertificateIssuedInRegistryEventHandler>
{
    private readonly RetryOptions retryOptions;

    public CertificateIssuedInRegistryEventHandlerConsumerDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CertificateIssuedInRegistryEventHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));

        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    }
}
