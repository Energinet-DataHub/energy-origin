using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace RegistryConnector.Worker.RoutingSlips;

public record MarkAsIssuedArguments(Guid CertificateId, MeteringPointType MeteringPointType);

public class MarkAsIssuedActivity : IExecuteActivity<MarkAsIssuedArguments>
{
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<MarkAsIssuedActivity> logger;

    public MarkAsIssuedActivity(ApplicationDbContext dbContext, ILogger<MarkAsIssuedActivity> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<MarkAsIssuedArguments> context)
    {
        Certificate? certificate = context.Arguments.MeteringPointType == MeteringPointType.Production
            ? await dbContext.ProductionCertificates.FindAsync(new object?[] { context.Arguments.CertificateId },
                context.CancellationToken)
            : await dbContext.ConsumptionCertificates.FindAsync(new object?[] { context.Arguments.CertificateId },
                context.CancellationToken);

        if (certificate == null)
        {
            logger.LogWarning($"Certificate with certificateId {context.Arguments.CertificateId} and meteringPointType {context.Arguments.MeteringPointType} not found.");
            return context.Completed();
        }

        if (certificate.IsIssued)
            return context.Completed();

        certificate.Issue();

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return context.Completed();
    }
}
