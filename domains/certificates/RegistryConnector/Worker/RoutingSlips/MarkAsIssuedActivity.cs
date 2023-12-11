using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;

namespace RegistryConnector.Worker.RoutingSlips;

public record MarkAsIssuedArguments(Guid CertificateId, MeteringPointType MeteringPointType);

public class MarkAsIssuedActivity : IExecuteActivity<MarkAsIssuedArguments>
{
    private readonly ApplicationDbContext dbContext;

    public MarkAsIssuedActivity(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<MarkAsIssuedArguments> context)
    {
        Certificate? certificate = context.Arguments.MeteringPointType == MeteringPointType.Production
            ? await dbContext.ProductionCertificates.FindAsync(new object?[] { context.Arguments.CertificateId },
                context.CancellationToken)
            : await dbContext.ConsumptionCertificates.FindAsync(new object?[] { context.Arguments.CertificateId },
                context.CancellationToken);

        if (certificate!.IsIssued)  //TODO: Handle if not found
            return context.Completed();

        certificate.Issue();

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return context.Completed();
    }
}
