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


public class IssueCertificateNotCompletedConsumer :
    IConsumer<IssueCertificateTerminated>
{
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<IssueCertificateNotCompletedConsumer> logger;

    public IssueCertificateNotCompletedConsumer(ApplicationDbContext dbContext, ILogger<IssueCertificateNotCompletedConsumer> logger)
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

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Certificate with certificateId {certificateId} rejected. Reason: {reason}", certificateId, rejectionReason);
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
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));

        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    }
}
