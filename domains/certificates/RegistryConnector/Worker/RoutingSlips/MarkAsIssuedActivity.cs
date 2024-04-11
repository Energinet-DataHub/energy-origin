using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RegistryConnector.Worker.RoutingSlips;

public record MarkAsIssuedArguments(Guid CertificateId, MeteringPointType MeteringPointType);

public class MarkAsIssuedActivity : IExecuteActivity<MarkAsIssuedArguments>
{
    private readonly CertificateDbContext dbContext;
    private readonly ILogger<MarkAsIssuedActivity> logger;

    public MarkAsIssuedActivity(CertificateDbContext dbContext, ILogger<MarkAsIssuedActivity> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<MarkAsIssuedArguments> context)
    {
        Certificate? certificate = context.Arguments.MeteringPointType == MeteringPointType.Production
            ? await dbContext.ProductionCertificates.FindAsync([context.Arguments.CertificateId],
                context.CancellationToken)
            : await dbContext.ConsumptionCertificates.FindAsync([context.Arguments.CertificateId],
                context.CancellationToken);

        if (certificate == null)
        {
            logger.LogWarning("Certificate with certificateId {context.Arguments.CertificateId} and meteringPointType {context.Arguments.MeteringPointType} not found.", context.Arguments.CertificateId, context.Arguments.MeteringPointType);
            return context.Terminate(new List<KeyValuePair<string, object>>
            {
                new("Reason", "Certificate not found")
            });
        }

        if (certificate.IsIssued)
            return context.Completed();

        certificate.Issue();

        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Certificate with certificateId {context.Arguments.CertificateId} and meteringPointType {context.Arguments.MeteringPointType} issued.", context.Arguments.CertificateId, context.Arguments.MeteringPointType);

        return context.Completed();
    }
}

public class MarkAsIssuedActivityDefinition : ExecuteActivityDefinition<MarkAsIssuedActivity, MarkAsIssuedArguments>
{
    private readonly RetryOptions retryOptions;

    public MarkAsIssuedActivityDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureExecuteActivity(
        IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<MarkAsIssuedActivity, MarkAsIssuedArguments> executeActivityConfigurator,
        IRegistrationContext context
        )
    {
        //endpointConfigurator.UseDelayedRedelivery(r => r.Interval(retryOptions.DefaultSecondLevelRetryCount, TimeSpan.FromDays(1)));

        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));

        endpointConfigurator.UseEntityFrameworkOutbox<CertificateDbContext>(context);
    }
}
