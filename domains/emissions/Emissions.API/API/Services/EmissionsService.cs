using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;

namespace API.Services;

public class EmissionsService : IEmissionsService
{
    readonly IDataSyncService dataSyncService;
    readonly IEnergiDataService energyDataService;
    readonly IEmissionsCalculator emissionsCalculator;
    readonly ISourcesCalculator sourcesCalculator;

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

    public async Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation)
    {
        //Get list of metering points
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var emissions = await energyDataService.GetEmissionsPerHour(dateFrom.ToDateTime(), dateTo.ToDateTime());

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, aggregation, meteringPoints.Where(mp => mp.Type == MeterType.Consumption));

        //Calculate total emission
        return emissionsCalculator.CalculateEmission(emissions, measurements, dateFrom, dateTo, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(AuthorizationContext context, long dateFrom, long dateTo,
        Aggregation aggregation, IEnumerable<MeteringPoint> meteringPoints)
    {
        List<TimeSeries> timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await dataSyncService.GetMeasurements(context, meteringPoint.GSRN,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime,
                DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime);

            timeSeries.Add(new TimeSeries(meteringPoint, measurements));
        }

        return timeSeries;
    }

    public async Task<EnergySourceResponse> GetSourceDeclaration(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation)
    {
        //Get list of metering points
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        //Get metering point time series
        var measurements = await GetTimeSeries(context, dateFrom, dateTo, aggregation, meteringPoints.Where(mp => mp.Type == MeterType.Consumption));

        var mixRecords = await energyDataService.GetResidualMixPerHour(dateFrom.ToDateTime(), dateTo.ToDateTime());

        return sourcesCalculator.CalculateSourceEmissions(measurements, mixRecords, aggregation);
    }
}