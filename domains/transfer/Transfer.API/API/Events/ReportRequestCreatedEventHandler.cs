using System;
using System.Text;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.Pdf.V1;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using DataContext.Models;

namespace API.Events;

public class ReportRequestCreatedEventHandler(
    IMediator mediator,
    ILogger<ReportRequestCreatedEventHandler> logger,
    IReportRepository reports,
    IUnitOfWork unitOfWork)
    : IConsumer<ReportRequestCreated>
{
    public async Task Consume(ConsumeContext<ReportRequestCreated> context)
    {
        var e = context.Message;

        logger.LogInformation(
            "Report request received for ReportId={ReportId}, Start={Start}, End={End}",
            e.ReportId,
            UnixTimestamp.Create(e.StartDate),
            UnixTimestamp.Create(e.EndDate));

        const string emptyHtml = "<!DOCTYPE html><html><body></body></html>";
        var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(emptyHtml));

        var pdfResult = await mediator.Send(
            new GeneratePdfCommand(base64Html),
            context.CancellationToken
        );

        if (!pdfResult.IsSuccess)
        {
            logger.LogError(
                "Failed to generate PDF for ReportId={ReportId}: {Error}",
                e.ReportId,
                pdfResult.ErrorContent);

            var failedReport = await reports.GetByIdAsync(e.ReportId, context.CancellationToken);
            if (failedReport != null && failedReport.Status == ReportStatus.Pending)
            {
                failedReport.MarkFailed();
                await reports.UpdateAsync(failedReport, context.CancellationToken);
                await unitOfWork.SaveAsync();
                logger.LogInformation("Report {ReportId} marked as Failed", e.ReportId);
            }
            return;
        }

        var report = await reports.GetByIdAsync(e.ReportId, context.CancellationToken)
                     ?? throw new InvalidOperationException($"Report {e.ReportId} not found.");

        report.MarkCompleted(pdfResult.PdfBytes!);

        await reports.UpdateAsync(report, context.CancellationToken);
        await unitOfWork.SaveAsync();

        logger.LogInformation("Report {ReportId} completed", e.ReportId);
    }
}

public class ReportRequestCreatedEventHandlerDefinition :
    ConsumerDefinition<ReportRequestCreatedEventHandler>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ReportRequestCreatedEventHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
