using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;
using DataContext.Models;

namespace API.Transfer.Api._Features_;

public class CreateReportRequestCommandHandler
    : IRequestHandler<CreateReportRequestCommand, Unit>
{
    private readonly IReportRepository _reports;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateReportRequestCommandHandler> _logger;
    private readonly IEnergyDataFetcher _dataFetcher;
    private readonly IHeadlinePercentageProcessor _percentageProcessor;
    private readonly IEnergySvgRenderer _svgRenderer;
    private readonly IOrganizationHeaderRenderer _headerRenderer;
    private readonly IHeadlinePercentageRenderer _headlinePercentageRenderer;
    private readonly ILogoRenderer _logoRenderer;
    private readonly IStyleRenderer _styleRenderer;
    private readonly IDetailsRenderer _detailsRenderer;
    private readonly IDisclaimerRenderer _disclaimerRenderer;

    public CreateReportRequestCommandHandler(
        IReportRepository reports,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<CreateReportRequestCommandHandler> logger,
        IEnergyDataFetcher dataFetcher,
        IHeadlinePercentageProcessor percentageProcessor,
        IEnergySvgRenderer svgRenderer,
        IOrganizationHeaderRenderer headerRenderer,
        IHeadlinePercentageRenderer headlinePercentageRenderer,
        ILogoRenderer logoRenderer,
        IStyleRenderer styleRenderer,
        IDetailsRenderer detailsRenderer,
        IDisclaimerRenderer disclaimerRenderer)
    {
        _reports = reports;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _logger = logger;
        _dataFetcher = dataFetcher;
        _percentageProcessor = percentageProcessor;
        _svgRenderer = svgRenderer;
        _headerRenderer = headerRenderer;
        _headlinePercentageRenderer = headlinePercentageRenderer;
        _logoRenderer = logoRenderer;
        _styleRenderer = styleRenderer;
        _detailsRenderer = detailsRenderer;
        _disclaimerRenderer = disclaimerRenderer;
    }

    public async Task<Unit> Handle(
        CreateReportRequestCommand request,
        CancellationToken cancellationToken)
    {
        var report = Report.Create(
            id: request.ReportId,
            organizationId: request.OrganizationId,
            organizationName: request.OrganizationName,
            organizationTin: request.OrganizationTin,
            startDate: request.StartDate,
            endDate: request.EndDate,
            language: PdfLanguageMapper.Map(request.Language));

        await _reports.AddAsync(report, cancellationToken);
        await _unitOfWork.SaveAsync();

        try
        {
            var from = DateTimeOffset.FromUnixTimeSeconds(request.StartDate.EpochSeconds);
            var to = DateTimeOffset.FromUnixTimeSeconds(request.EndDate.EpochSeconds);
            var (consumption, strictProd, allProd) =
                await _dataFetcher.GetAsync(request.OrganizationId, from, to, cancellationToken);

            // Process into hourly aggregates
            var hourlyData = EnergyDataProcessor.ToHourly(consumption, strictProd, allProd);

            // Calculate coverage headline
            var headlinePercent = _percentageProcessor.Calculate(hourlyData);
            var periodFromTo = $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";

            // Render HTML fragments
            var organizationHeaderHtml = _headerRenderer.Render(
                HttpUtility.HtmlEncode(request.OrganizationName.Value),
                HttpUtility.HtmlEncode(request.OrganizationTin.Value),
                report.Language);
            var headlineHtml = _headlinePercentageRenderer.Render(headlinePercent, periodFromTo, report.Language);
            var svgHtml = _svgRenderer.Render(hourlyData, report.Language).Svg;
            var logoHtml = _logoRenderer.Render();
            var styleHtml = _styleRenderer.Render();
            var detailsHtml = _detailsRenderer.Render(report.Language);
            var disclaimerHtml = _disclaimerRenderer.Render(report.Language);

            if (string.IsNullOrEmpty(svgHtml) || !svgHtml.Contains("<svg"))
            {
                _logger.LogWarning("SVG content appears to be invalid for ReportId={ReportId}", report.Id);
                svgHtml = "<p>Chart data could not be displayed</p>"; // Fallback content
            }

            // Assemble full HTML
            var fullHtml = $$"""
                             <!DOCTYPE html>
                             <html>
                               <head>
                                 <meta charset="UTF-8">
                                 <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                 {{styleHtml}}
                                 <title>Report</title>
                             </head>
                               <body>
                                 {{organizationHeaderHtml}}
                                 <div class="chart">
                                 {{headlineHtml}}
                                 {{svgHtml}}
                                 </div>
                             {{logoHtml}}
                             {{detailsHtml}}
                             {{disclaimerHtml}}
                               </body>
                             </html>
                             """;

            // Convert to base64 and generate PDF
            var base64Html = Convert.ToBase64String(Encoding.UTF8.GetBytes(fullHtml));
            var pdfResult = await _mediator.Send(
                new GeneratePdfCommand(base64Html),
                cancellationToken);

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
                _logger.LogInformation(
                    "Report {ReportId} completed successfully", report.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report {ReportId}", report.Id);
            report.MarkFailed();
        }

        // Persist final status
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
    UnixTimestamp EndDate,
    string Language
) : IRequest<Unit>;

internal static class PdfLanguageMapper
{
    public static Language Map(string lang)
    {
        var normalized = lang.Trim().ToLowerInvariant();

        return normalized switch
        {
            "da" or "da-dk" => Language.Danish,
            _ => Language.English
        };
    }
}
