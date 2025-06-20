using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using API.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;

namespace API.Transfer.Api._Features_;

public record PopulateReportCommand(
    Guid ReportId
) : IRequest<Unit>;

public class PopulateReportCommandHandler
    : IRequestHandler<PopulateReportCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<PopulateReportCommandHandler> _logger;
    private readonly IEnergyDataFetcher _dataFetcher;
    private readonly IEnergyDataFormatter _dataFormatter;
    private readonly IHeadlinePercentageProcessor _percentageProcessor;
    private readonly IMunicipalityPercentageProcessor _municipalityPercentageProcessor;
    private readonly IEnergySvgRenderer _svgRenderer;
    private readonly IOrganizationHeaderRenderer _headerRenderer;
    private readonly IHeadlinePercentageRenderer _percentageRenderer;
    private readonly IMunicipalityPercentageRenderer _municipalityPercentageRenderer;
    private readonly ILogoRenderer _logoRenderer;
    private readonly IStyleRenderer _styleRenderer;

    public PopulateReportCommandHandler(
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<PopulateReportCommandHandler> logger,
        IEnergyDataFetcher dataFetcher,
        IEnergyDataFormatter dataFormatter,
        IHeadlinePercentageProcessor percentageProcessor,
        IMunicipalityPercentageProcessor municipalityPercentageProcessor,
        IEnergySvgRenderer svgRenderer,
        IOrganizationHeaderRenderer headerRenderer,
        IHeadlinePercentageRenderer percentageRenderer,
        IMunicipalityPercentageRenderer municipalityPercentageRenderer,
        ILogoRenderer logoRenderer,
        IStyleRenderer styleRenderer)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _logger = logger;
        _dataFetcher = dataFetcher;
        _dataFormatter = dataFormatter;
        _percentageProcessor = percentageProcessor;
        _municipalityPercentageProcessor = municipalityPercentageProcessor;
        _svgRenderer = svgRenderer;
        _headerRenderer = headerRenderer;
        _percentageRenderer = percentageRenderer;
        _municipalityPercentageRenderer = municipalityPercentageRenderer;
        _logoRenderer = logoRenderer;
        _styleRenderer = styleRenderer;
    }

    public async Task<Unit> Handle(
        PopulateReportCommand request,
        CancellationToken cancellationToken)
    {
        var report = await _unitOfWork.ReportRepository.GetByIdAsync(request.ReportId, cancellationToken);
        if (report == null)
        {
            throw new ArgumentNullException($"Cannot generate report with unknown report id {request.ReportId}");
        }

        try
        {
            var from = DateTimeOffset.FromUnixTimeSeconds(report.StartDate.EpochSeconds);
            var to = DateTimeOffset.FromUnixTimeSeconds(report.EndDate.EpochSeconds);
            var (consumptionRaw, claims) = await _dataFetcher.GetAsync(report.OrganizationId, from, to, cancellationToken);

            var (consumption, strictProd, allProd) = _dataFormatter.Format(consumptionRaw, claims);

            // Process into hourly aggregates
            var hourlyData = EnergyDataProcessor.ToHourly(consumption, strictProd, allProd);

            // Calculate coverage headline
            var headlinePercent = _percentageProcessor.Calculate(hourlyData);
            var periodLabel = $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";
            var municipalities = _municipalityPercentageProcessor.Calculate(claims);

            // Render HTML fragments
            var headerHtml = _headerRenderer.Render(
                HttpUtility.HtmlEncode(report.OrganizationName.Value),
                HttpUtility.HtmlEncode(report.OrganizationTin.Value));
            var headlineHtml = _percentageRenderer.Render(headlinePercent, periodLabel);
            var svgHtml = _svgRenderer.Render(hourlyData).Svg;
            var logoHtml = _logoRenderer.Render();
            var styleHtml = _styleRenderer.Render();
            var municipalitiesHtml = _municipalityPercentageRenderer.Render(municipalities);

            if (string.IsNullOrEmpty(svgHtml) || !svgHtml.Contains("<svg"))
            {
                _logger.LogWarning("SVG content appears to be invalid for ReportId={ReportId}", report.Id);
                svgHtml = "<p>Chart data could not be displayed</p>"; // Fallback content
            }

            var watermarkHtml = report.IsNormal
            ? string.Empty
            : "<div class=\"watermark\">TRIAL</div>";

            // Assemble full HTML
            var fullHtml = $$"""
            <!DOCTYPE html>
            <html>
              <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                {{styleHtml}}
                <title>Granulære Oprindelsesgarantier</title>
              </head>
              <body>
                {{watermarkHtml}}
                <div class="content">
                  {{headerHtml}}
                  <div class="chart">
                    {{headlineHtml}}
                    {{svgHtml}}
                  </div>
                  <div class="details">
                    <p class="description">Granulære Oprindelsesgarantier er udelukkende udstedt på basis af sol- og vindproduktion.
                       Herunder kan du se en fordeling af geografisk oprindelse, teknologityper samt andelen fra statsstøttede
                       producenter.</p>
                    <div class="sections">
                      <div class="section-column">
                        {{municipalitiesHtml}}
                      </div>
                      <div class="section-column">
                        <h6 class="section-title">Teknologi</h6>
                        <ul>
                          <li>Solenergi: 38%</li>
                          <li>Vindenergi: 62%</li>
                        </ul>
                      </div>
                      <div class="section-column">
                        <h6 class="section-title">Andel fra statsstøttede producenter</h6>
                        <ul>
                          <li>Ikke statsstøttede: 95%</li>
                          <li>Statsstøttede: 5%</li>
                        </ul>
                      </div>
                    </div>
                  </div>
                  {{logoHtml}}
                  <div class="disclaimer">
                    <p>Data grundlag & Godkendelse. Vivamus sagittis lacus vel augue laoreet rutrum faucibus dolor auctor.
                       Fusce dapibus, tellus ac cursus commodo, tortor mauris condimentum nibh, ut fermentum massa justo
                       sit amet risus. Duis mollis, est non commodo luctus, nisi erat porttitor ligula, eget lacinia odio
                       sem nec elit.</p>
                  </div>
                </div>
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
        await _unitOfWork.ReportRepository.UpdateAsync(report, cancellationToken);
        await _unitOfWork.SaveAsync();

        return Unit.Value;
    }
}
