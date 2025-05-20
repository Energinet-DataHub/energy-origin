using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Transfer.Api._Features_;

public class CreateReportRequestCommandHandler
    : IRequestHandler<CreateReportRequestCommand, Unit>
{
    private readonly IReportRepository _reports;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateReportRequestCommandHandler> _logger;

    public CreateReportRequestCommandHandler(
        IReportRepository reports,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<CreateReportRequestCommandHandler> logger)
    {
        _reports = reports;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Unit> Handle(
        CreateReportRequestCommand request,
        CancellationToken cancellationToken)
    {
        var duration = request.EndDate.EpochSeconds - request.StartDate.EpochSeconds;
        if (duration > UnixTimestamp.SecondsPerDay * 365)
            throw new BusinessException("Date range cannot exceed 1 year.");

        var report = Report.Create(
            request.ReportId,
            request.OrganizationId,
            request.StartDate,
            request.EndDate);

        await _reports.AddAsync(report, cancellationToken);
        await _unitOfWork.SaveAsync();

        const string emptyHtml = "<!DOCTYPE html><html><body></body></html>";
        var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(emptyHtml));

        var pdfResult = await _mediator.Send(
            new GeneratePdfCommand(base64Html),
            cancellationToken);

        if (!pdfResult.IsSuccess)
        {
            _logger.LogError(
                "Failed to generate PDF for ReportId={ReportId}: {Error}",
                report.Id, pdfResult.ErrorContent);

            report.MarkFailed();
            await _reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Report {ReportId} marked as Failed", report.Id);
            return Unit.Value;
        }

        report.MarkCompleted(pdfResult.PdfBytes!);
        await _reports.UpdateAsync(report, cancellationToken);
        await _unitOfWork.SaveAsync();

        _logger.LogInformation("Report {ReportId} completed", report.Id);
        return Unit.Value;
    }
}

public record CreateReportRequestCommand(
    Guid ReportId,
    OrganizationId OrganizationId,
    UnixTimestamp StartDate,
    UnixTimestamp EndDate
) : IRequest<Unit>;
