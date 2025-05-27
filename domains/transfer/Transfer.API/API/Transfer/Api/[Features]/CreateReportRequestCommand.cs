using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;
using DataContext.Models;

namespace API.Transfer.Api._Features_
{
    public class CreateReportRequestCommandHandler
        : IRequestHandler<CreateReportRequestCommand, Unit>
    {
        private readonly IReportRepository _reports;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILogger<CreateReportRequestCommandHandler> _logger;
        private readonly EnergyDataFetcher _dataFetcher;
        private readonly IHeadlinePercentageProcessor _percentageProcessor;
        private readonly IEnergySvgRenderer _svgRenderer;
        private readonly IOrganizationHeaderRenderer _headerRenderer;
        private readonly IHeadlinePercentageRenderer _percentageRenderer;

        public CreateReportRequestCommandHandler(
            IReportRepository reports,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILogger<CreateReportRequestCommandHandler> logger,
            EnergyDataFetcher dataFetcher,
            IHeadlinePercentageProcessor percentageProcessor,
            IEnergySvgRenderer svgRenderer,
            IOrganizationHeaderRenderer headerRenderer,
            IHeadlinePercentageRenderer percentageRenderer)
        {
            _reports = reports;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
            _dataFetcher = dataFetcher;
            _percentageProcessor = percentageProcessor;
            _svgRenderer = svgRenderer;
            _headerRenderer = headerRenderer;
            _percentageRenderer = percentageRenderer;
        }

        public async Task<Unit> Handle(
            CreateReportRequestCommand request,
            CancellationToken cancellationToken)
        {
            // Persist initial report request
            var report = Report.Create(
                id: request.ReportId,
                organizationId: request.OrganizationId,
                organizationName: request.OrganizationName,
                organizationTin: request.OrganizationTin,
                startDate: request.StartDate,
                endDate: request.EndDate);

            await _reports.AddAsync(report, cancellationToken);
            await _unitOfWork.SaveAsync();

            try
            {
                // Fetch data
                var from = DateTimeOffset.FromUnixTimeSeconds(request.StartDate.EpochSeconds);
                var to = DateTimeOffset.FromUnixTimeSeconds(request.EndDate.EpochSeconds);
                var (consumption, strictProd, allProd) = await _dataFetcher.GetAsync(
                    request.OrganizationId, from, to, cancellationToken);

                // Process into hourly buckets
                var hourlyData = EnergyDataProcessor.ToHourly(consumption, strictProd, allProd);

                // Calculate headline percentage
                var headlinePercent = _percentageProcessor.Calculate(hourlyData);
                var periodLabel = $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";

                // Render HTML fragments
                var headerHtml = _headerRenderer.Render(request.OrganizationName.Value, request.OrganizationTin.Value);
                var headlineHtml = _percentageRenderer.Render(headlinePercent, periodLabel);
                var svgHtml = _svgRenderer.Render(hourlyData).Svg;

                // Assemble full HTML
                var fullHtml = $"""
<!DOCTYPE html>
<html>
  <head><meta charset='utf-8'/></head>
  <body>
    {headerHtml}
    {headlineHtml}
    {svgHtml}
  </body>
</html>
""";

                var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(fullHtml));

                // Generate PDF
                var pdfResult = await _mediator.Send(
                    new GeneratePdfCommand(base64Html), cancellationToken);

                if (!pdfResult.IsSuccess)
                {
                    _logger.LogError(
                        "Failed to generate PDF for ReportId={ReportId}: {Error}",
                        report.Id, pdfResult.ErrorContent);

                    report.MarkFailed();
                }
                else
                {
                    report.MarkCompleted(pdfResult.PdfBytes!);
                    _logger.LogInformation("Report {ReportId} completed", report.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report {ReportId}", report.Id);
                report.MarkFailed();
            }

            // Update report status
            await _reports.UpdateAsync(report, cancellationToken);
            await _unitOfWork.SaveAsync();

            return Unit.Value;
        }
    }

    public record CreateReportRequestCommand(
        Guid ReportId,
        OrganizationId OrganizationId,
        OrganizationName OrganizationName,
        Tin OrganizationTin,
        UnixTimestamp StartDate,
        UnixTimestamp EndDate
    ) : IRequest<Unit>;
}
