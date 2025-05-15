using System;
using System.Text;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using DataContext.Models;
using MassTransit;
using MediatR;

namespace API.Transfer.Api._Features_;

public abstract class GenerateReportCommandConsumer(
    IReportRepository reportRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator)
    : IConsumer<GenerateReportCommand>
{
    public async Task Consume(ConsumeContext<GenerateReportCommand> context)
    {
        var command = context.Message;
        var report = await reportRepository.GetByIdAsync(command.ReportId, context.CancellationToken);

        if (report == null) return;

        try
        {
            var html = "<h1>Hello World Report</h1>" +
                       $"<p>Report Period: {command.StartDate:d} - {command.EndDate:d}</p>";

            var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));

            var pdfResult = await mediator.Send(
                new GeneratePdfCommand(base64Html),
                context.CancellationToken
            );

            if (pdfResult.IsSuccess && pdfResult.PdfBytes != null)
            {
                report.Content = pdfResult.PdfBytes;
                report.Status = ReportStatus.Completed;
            }
            else
            {
                report.Status = ReportStatus.Failed;
            }
        }
        catch
        {
            report.Status = ReportStatus.Failed;
        }

        await reportRepository.UpdateAsync(report, context.CancellationToken);
        await unitOfWork.SaveAsync();

        if (report.Status == ReportStatus.Completed)
        {
            await context.Publish(new ReportGenerationCompleted(report.Id));
        }
        else
        {
            await context.Publish(new ReportGenerationFailed(report.Id, "PDF generation failed"));
        }
    }
}


public record GenerateReportCommand(
    Guid ReportId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate
);

public record ReportGenerationCompleted(Guid ReportId);

public record ReportGenerationFailed(Guid ReportId, string Error);
