using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataContext;
using DataContext.ValueObjects;
using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RegistryConnector.Worker.RoutingSlips;

public class IssueCertificateTerminated
{
    public Guid CertificateId { get; set; }
    public MeteringPointType MeteringPointType { get; set; }
    /// <summary>
    /// Populated by MassTransit.
    /// The variables that were present once the routing slip completed, can be used
    /// to capture the output of the slip - real events should likely be used for real
    /// completion items but this is useful for some cases.
    /// </summary>
    public IDictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
}

public class IssueCertificateFaulted
{
    public Guid CertificateId { get; set; }
    public MeteringPointType MeteringPointType { get; set; }
    /// <summary>
    /// Populated by MassTransit.
    /// The exception information from the faulting activities.
    /// </summary>
    public ActivityException[] ActivityExceptions { get; set; } = Array.Empty<ActivityException>();
}


public class IssueCertificateNotCompletedConsumer :
    IConsumer<IssueCertificateTerminated>,
    IConsumer<IssueCertificateFaulted>
{
    private readonly CertificateDbContext dbContext;
    private readonly ILogger<IssueCertificateNotCompletedConsumer> logger;

    public IssueCertificateNotCompletedConsumer(CertificateDbContext dbContext, ILogger<IssueCertificateNotCompletedConsumer> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<IssueCertificateTerminated> context)
    {
        var message = context.Message;
        var rejectionReason = $"Terminated: {message.Variables["Reason"]}";

        await Reject(message.MeteringPointType, message.CertificateId, rejectionReason);
    }

    public async Task Consume(ConsumeContext<IssueCertificateFaulted> context)
    {
        var message = context.Message;
        var exceptionInfo = message.ActivityExceptions[0].ExceptionInfo;
        var rejectionReason = $"Faulted: {exceptionInfo.ExceptionType} - {exceptionInfo.Message}";

        await Reject(message.MeteringPointType, message.CertificateId, rejectionReason);
    }

    private async Task Reject(MeteringPointType meteringPointType, Guid certificateId, string rejectionReason)
    {
        if (meteringPointType == MeteringPointType.Production)
        {
            var productionCertificate = await dbContext.ProductionCertificates.FindAsync(certificateId);

            if (productionCertificate == null)
            {
                logger.LogWarning("Production certificate with certificateId {certificateId} not found.", certificateId);
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
            consumptionCertificate.Reject(rejectionReason);
        }

        var changed = await dbContext.SaveChangesAsync();
        logger.LogInformation("{changed}", changed);
    }
}

public class IssueCertificateNotCompletedConsumerDefinition : ConsumerDefinition<IssueCertificateNotCompletedConsumer>
{
    private readonly RetryOptions retryOptions;

    public IssueCertificateNotCompletedConsumerDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<IssueCertificateNotCompletedConsumer> consumerConfigurator,
        IRegistrationContext context
        )
    {
        //endpointConfigurator.UseDelayedRedelivery(r => r.Interval(retryOptions.DefaultSecondLevelRetryCount, TimeSpan.FromDays(1)));

        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));

        endpointConfigurator.UseEntityFrameworkOutbox<CertificateDbContext>(context);
    }
}
