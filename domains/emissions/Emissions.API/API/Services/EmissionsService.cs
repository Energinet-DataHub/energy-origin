using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class EmissionsService : IEmissionsService
{
    readonly IDataSyncService dataSyncService;
    readonly IEmissionDataService emissionDataService;
    readonly IEmissionsCalculator emissionsCalculator;
    readonly ISourcesCalculator sourcesCalculator;

    public EmissionsService(IDataSyncService dataSyncService, IEmissionDataService emissionDataService, IEmissionsCalculator emissionsCalculator, ISourcesCalculator sourcesCalculator)
    {
        this.dataSyncService = dataSyncService;
        this.emissionDataService = emissionDataService;
        this.emissionsCalculator = emissionsCalculator;
        this.sourcesCalculator = sourcesCalculator;
    }

    public async Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext authorizationContext,
        long dateFrom, long dateTo, Aggregation aggregation)
    {
        //Get list of metering points
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(authorizationContext);

        //Get emissions in date range
        var emissions = await emissionDataService.GetEmissionsPerHour(dateFrom.ToUtcDateTime(), dateTo.ToUtcDateTime());

        //Get metering point time series
        var measurements = await GetTimeSeries(authorizationContext, dateFrom, dateTo, aggregation, meteringPoints);

        //Calculate total emission
        return emissionsCalculator.CalculateEmission(emissions.Result.EmissionRecords, measurements, dateFrom, dateTo, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(AuthorizationContext authorizationContext, long dateFrom, long dateTo,
        Aggregation aggregation, IEnumerable<MeteringPoint> meteringPoints)
    {
        List<TimeSeries> timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await dataSyncService.GetMeasurements(authorizationContext, meteringPoint.GSRN,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime,
                DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime, aggregation);

            timeSeries.Add(new TimeSeries(meteringPoint, measurements));
        }

        return timeSeries;
    }

    public async Task<EnergySourceResponse> GetSourceDeclaration(AuthorizationContext authorizationContext, long dateFrom, long dateTo, Aggregation aggregation)
    {
        //Get list of metering points
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(authorizationContext);

        //Get metering point time series
        var measurements = await GetTimeSeries(authorizationContext, dateFrom, dateTo, aggregation, meteringPoints);

        //Get Declaration, Production types and Emissions per Hour
        var declaration = await emissionDataService.GetDeclarationProduction(dateFrom.ToUtcDateTime(), dateTo.ToUtcDateTime());

        return sourcesCalculator.CalculateSourceEmissions(measurements, declaration, dateFrom, dateTo, aggregation);

    }
}
