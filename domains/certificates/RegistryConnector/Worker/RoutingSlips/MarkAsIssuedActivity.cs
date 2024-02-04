using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RegistryConnector.Worker.Metrics;

namespace RegistryConnector.Worker.RoutingSlips;

public record MarkAsIssuedArguments(Guid CertificateId, MeteringPointType MeteringPointType);

public class MarkAsIssuedActivity(ApplicationDbContext dbContext, ILogger<MarkAsIssuedActivity> logger)
    : IExecuteActivity<MarkAsIssuedArguments>
{
    public async Task<ExecutionResult> Execute(ExecuteContext<MarkAsIssuedArguments> context)
    {
        Certificate? certificate = context.Arguments.MeteringPointType == MeteringPointType.Production
            ? await dbContext.ProductionCertificates.FindAsync(context.Arguments.CertificateId, context.CancellationToken)
            : await dbContext.ConsumptionCertificates.FindAsync(context.Arguments.CertificateId, context.CancellationToken);

        if (certificate == null)
        {
            logger.LogWarning("Certificate with certificateId {CertificateId} and meteringPointType {MeteringPointType} not found", context.Arguments.CertificateId, context.Arguments.MeteringPointType);
            return context.Terminate(new List<KeyValuePair<string, object>>
            {
                new("Reason", "Certificate not found")
            });
        }

        if (certificate.IsIssued)
            return context.Completed();

        certificate.Issue();

        try
        {
            await dbContext.SaveChangesAsync(context.CancellationToken);

            CertificateMetrics.CertificateIssued();

            logger.LogInformation("Certificate with certificateId {CertificateId} and meteringPointType {MeteringPointType} issued", context.Arguments.CertificateId, context.Arguments.MeteringPointType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save changes for certificateId {CertificateId} and meteringPointType {MeteringPointType}", context.Arguments.CertificateId, context.Arguments.MeteringPointType);
            return context.Terminate(new List<KeyValuePair<string, object>>
            {
                new("Reason", "Failed to issue certificate due to database save error")
            });
        }

        return context.Completed();
    }
}

public class MarkAsIssuedActivityDefinition(IOptions<RetryOptions> options)
    : ExecuteActivityDefinition<MarkAsIssuedActivity, MarkAsIssuedArguments>
{
    private readonly RetryOptions retryOptions = options.Value;

    protected override void ConfigureExecuteActivity(
        IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<MarkAsIssuedActivity, MarkAsIssuedArguments> executeActivityConfigurator,
        IRegistrationContext context
        )
    {
        //endpointConfigurator.UseDelayedRedelivery(r => r.Interval(retryOptions.DefaultSecondLevelRetryCount, TimeSpan.FromDays(1)));

        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));

        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
    }
}
