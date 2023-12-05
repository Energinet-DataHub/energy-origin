using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using DataContext.ValueObjects;
using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.Extensions.Logging;

namespace RegistryConnector.Worker.RoutingSlip;

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
        var rejectionReason = $"Terminated: {message.Variables["Reason"]}"; //TODO: Constant

        await Reject(message.MeteringPointType, message.CertificateId, rejectionReason);
    }

    public async Task Consume(ConsumeContext<IssueCertificateFaulted> context)
    {
        var message = context.Message;
        var exceptionInfo = message.ActivityExceptions.First().ExceptionInfo;
        var rejectionReason = $"Faulted: {exceptionInfo.ExceptionType} - {exceptionInfo.Message}";

        await Reject(message.MeteringPointType, message.CertificateId, rejectionReason);
    }

    private async Task Reject(MeteringPointType meteringPointType, Guid certificateId, string rejectionReason)
    {
        if (meteringPointType == MeteringPointType.Production)
        {
            var productionCertificate = await dbContext.ProductionCertificates.FindAsync(certificateId);
            productionCertificate!.Reject(rejectionReason); //TODO: Handle if not found
        }
        else
        {
            var consumptionCertificate = await dbContext.ConsumptionCertificates.FindAsync(certificateId);
            consumptionCertificate!.Reject(rejectionReason); //TODO: Handle if not found
        }

        var changed = await dbContext.SaveChangesAsync();
        logger.LogInformation("{changed}", changed);
    }
}
