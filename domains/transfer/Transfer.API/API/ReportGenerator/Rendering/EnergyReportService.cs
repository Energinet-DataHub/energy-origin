using System;
using System.Threading;
using System.Threading.Tasks;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.Transfer.Api.Repository;
using EnergyOrigin.Domain.ValueObjects;

namespace API.ReportGenerator.Rendering;

public class EnergyReportService
{
    private readonly EnergyDataFetcher _dataFetcher;
    private readonly IEnergyDataFormatter _dataFormatter;
    private readonly IEnergySvgRenderer _energySvgRenderer;
    private readonly IHeadlinePercentageProcessor _percentageProcessor;
    private readonly IHeadlinePercentageRenderer _percentageRenderer;
    private readonly IReportRepository _reportRepository;
    IOrganizationHeaderRenderer _organizationHeaderRenderer;

    public EnergyReportService(
        EnergyDataFetcher dataFetcher,
        IEnergyDataFormatter dataFormatter,
        IEnergySvgRenderer energySvgRenderer,
        IHeadlinePercentageProcessor percentageProcessor,
        IHeadlinePercentageRenderer percentageRenderer,
        IReportRepository reportRepository,
        IOrganizationHeaderRenderer organizationHeaderRenderer)
    {
        _dataFetcher = dataFetcher;
        _dataFormatter = dataFormatter;
        _energySvgRenderer = energySvgRenderer;
        _percentageProcessor = percentageProcessor;
        _percentageRenderer = percentageRenderer;
        _reportRepository = reportRepository;
        _organizationHeaderRenderer = organizationHeaderRenderer;
    }

    public async Task<string> GenerateHeadlinePercentageReportAsync(
        OrganizationId orgId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        // Step 1: Fetch the raw data
        var (consumptionRaw, claims) =
            await _dataFetcher.GetAsync(orgId, from, to, ct);

        var (consumption, strictProduction, allProduction) = _dataFormatter.Format(consumptionRaw, claims);

        // Step 2: Transform the data into HourlyEnergy objects
        var hourlyEnergy = EnergyDataProcessor.ToHourly(
            consumption, strictProduction, allProduction);

        // Step 3: Calculate the percentage
        double percentage = _percentageProcessor.Calculate(hourlyEnergy);

        // Step 4: Generate the HTML
        string periodLabel = $"{from:MMM d} - {to:MMM d, yyyy}";
        string html = _percentageRenderer.Render(percentage, periodLabel);

        return html;
    }
}
