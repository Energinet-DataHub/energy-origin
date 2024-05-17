using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.ValueObjects;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RegistryConnector.Worker.EventHandlers;

public record CertificateFailedInRegistryEvent
{
    public required string RejectReason { get; init; }
    public required Guid CertificateId { get; init; }
    public required MeteringPointType MeteringPointType { get; init; }
}

public class CertificateFailedInRegistryEventHandler : IConsumer<CertificateFailedInRegistryEvent>
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly ILogger<CertificateFailedInRegistryEventHandler> logger;

    public CertificateFailedInRegistryEventHandler(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger<CertificateFailedInRegistryEventHandler> logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateFailedInRegistryEvent> context)
    {
        var message = context.Message;

        await Reject(message.MeteringPointType, message.CertificateId, message.RejectReason);
    }

    private async Task Reject(MeteringPointType meteringPointType, Guid certificateId, string rejectionReason)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        if (meteringPointType == MeteringPointType.Production)
        {
            var productionCertificate = await dbContext.ProductionCertificates.FindAsync(certificateId);

            if (productionCertificate == null)
            {
                logger.LogWarning("Production certificate with certificateId {certificateId} not found.", certificateId);
                return;
            }

            if (productionCertificate.IsRejected)
            {
                logger.LogWarning("Production certificate with certificateId {certificateId} already rejected.", certificateId);
                return;
            }

            productionCertificate.Reject(rejectionReason);
        }
        else
        {
            var consumptionCertificate = await dbContext.ConsumptionCertificates.FindAsync(certificateId);

            if (consumptionCertificate == null)
            {
                logger.LogWarning("Consumption certificate with certificateId {certificateId} not found.", certificateId);
                return;
            }

            if (consumptionCertificate.IsRejected)
            {
                logger.LogWarning("Consumption certificate with certificateId {certificateId} already rejected.", certificateId);
                return;
            }

            consumptionCertificate.Reject(rejectionReason);
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Certificate with certificateId {certificateId} rejected", certificateId);
    }
}

public class CertificateFailedInRegistryEventHandlerConsumerDefinition : ConsumerDefinition<CertificateFailedInRegistryEventHandler>
{
    private readonly RetryOptions retryOptions;

    public CertificateFailedInRegistryEventHandlerConsumerDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CertificateFailedInRegistryEventHandler> consumerConfigurator,
        IRegistrationContext context
    )
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
