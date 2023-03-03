using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class EmissionsService : IEmissionsService
{
    private readonly IDataSyncService dataSyncService;
    private readonly IEnergiDataService energyDataService;
    private readonly IEmissionsCalculator emissionsCalculator;
    private readonly ISourcesCalculator sourcesCalculator;

    public EmissionsService(
        IDataSyncService dataSyncService,
        IEnergiDataService energyDataService,
        IEmissionsCalculator emissionsCalculator,
        ISourcesCalculator sourcesCalculator)
    {
        this.dataSyncService = dataSyncService;
        this.energyDataService = energyDataService;
        this.emissionsCalculator = emissionsCalculator;
        this.sourcesCalculator = sourcesCalculator;
    }

    public async Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, TimeZoneInfo timeZone, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var emissions = await energyDataService.GetEmissionsPerHour(dateFrom, dateTo);

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, meteringPoints.Where(mp => mp.Type == MeterType.Consumption));

        return emissionsCalculator.CalculateEmission(emissions, measurements, timeZone, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(
        AuthorizationContext context,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        IEnumerable<MeteringPoint> meteringPoints)
    {
        var timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await dataSyncService.GetMeasurements(context, meteringPoint.GSRN, dateFrom, dateTo);

            timeSeries.Add(new TimeSeries(meteringPoint, measurements));
        }
        return timeSeries;
    }

    public async Task<EnergySourceResponse> GetSourceDeclaration(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, TimeZoneInfo timeZone, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, meteringPoints.Where(mp => mp.Type == MeterType.Consumption));

        var mixRecords = await energyDataService.GetResidualMixPerHour(dateFrom, dateTo);

        return sourcesCalculator.CalculateSourceEmissions(mixRecords, measurements, timeZone, aggregation);
    }
}
