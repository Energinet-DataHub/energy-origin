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
    private readonly IHeadlinePercentageRenderer _percentageRenderer;
    private readonly ILogoRenderer _logoRenderer;
    private readonly IStyleRenderer _styleRenderer;

    public CreateReportRequestCommandHandler(
        IReportRepository reports,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<CreateReportRequestCommandHandler> logger,
        IEnergyDataFetcher dataFetcher,
        IHeadlinePercentageProcessor percentageProcessor,
        IEnergySvgRenderer svgRenderer,
        IOrganizationHeaderRenderer headerRenderer,
        IHeadlinePercentageRenderer percentageRenderer,
        ILogoRenderer logoRenderer,
        IStyleRenderer styleRenderer)
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
        _logoRenderer = logoRenderer;
        _styleRenderer = styleRenderer;
    }

    public async Task<Unit> Handle(
        CreateReportRequestCommand request,
        CancellationToken cancellationToken)
    {
        // Create and persist a pending report
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
            // Fetch raw data
            var from = DateTimeOffset.FromUnixTimeSeconds(request.StartDate.EpochSeconds);
            var to = DateTimeOffset.FromUnixTimeSeconds(request.EndDate.EpochSeconds);
            var (consumption, strictProd, allProd) =
                await _dataFetcher.GetAsync(request.OrganizationId, from, to, cancellationToken);

            // Process into hourly aggregates
            var hourlyData = EnergyDataProcessor.ToHourly(consumption, strictProd, allProd);

            // Calculate coverage headline
            var headlinePercent = _percentageProcessor.Calculate(hourlyData);
            var periodLabel = $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";

            // Render HTML fragments
            var headerHtml = _headerRenderer.Render(
                HttpUtility.HtmlEncode(request.OrganizationName.Value),
                HttpUtility.HtmlEncode(request.OrganizationTin.Value));
            var headlineHtml = _percentageRenderer.Render(headlinePercent, periodLabel);
            var svgHtml = _svgRenderer.Render(hourlyData).Svg;
            var logoHtml = _logoRenderer.Render();

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
                                 <title>Granulære Oprindelsesgarantier</title>

                             </head>
                               <body>
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
                                         <h6 class="section-title">Kommuner</h6>
                                         <ul>
                                             <li>Aabenraa: 50%</li>
                                             <li>Ringkøbing-Skjern: 25%</li>
                                             <li>Lolland: 20%</li>
                                             <li>Andre kommuner: 5%</li>
                                         </ul>
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
                                            sem nec elit. </p>
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
