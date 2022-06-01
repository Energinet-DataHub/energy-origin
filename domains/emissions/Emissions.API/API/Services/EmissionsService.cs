using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class EmissionsService : IEmissionsService
{
    readonly IDataSyncService dataSyncService;
    readonly IEnergiDataService energiDataService;
    readonly IEmissionsCalculator emissionsCalculator;

    public EmissionsService(IDataSyncService dataSyncService, IEnergiDataService energiDataService, IEmissionsCalculator emissionsCalculator)
    {
        this.dataSyncService = dataSyncService;
        this.energiDataService = energiDataService;
        this.emissionsCalculator = emissionsCalculator;
    }

    public async Task<IEnumerable<Emissions>> GetTotalEmissions(AuthorizationContext authorizationContext,
        long dateFrom, long dateTo, Aggregation aggregation)
    {
        //Get list of metering points
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(authorizationContext);

        //Get emissions in date range
        var emissions = await energiDataService.GetEmissionsPerHour(DateTimeUtil.ToUtcDateTime(dateFrom), DateTimeUtil.ToUtcDateTime(dateTo));

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


}