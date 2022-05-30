using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class EmissionsService : IEmissionsService
{
    readonly IDataSyncService _dataSyncService;
    private readonly IEnergiDataService _energiDataService;
    private readonly IEmissionsCalculator _emissionsCalculator;

    public EmissionsService(IDataSyncService dataSyncService, IEnergiDataService energiDataService, IEmissionsCalculator emissionsCalculator)
    {
        _dataSyncService = dataSyncService;
        _energiDataService = energiDataService;
        _emissionsCalculator = emissionsCalculator;
    }

    public async Task<IEnumerable<Emissions>> GetTotalEmissions(AuthorizationContext authorizationContext,
        long dateFrom, long dateTo, Aggregation aggregation)
    {
        //Get list of metering points
        var meteringPoints = await _dataSyncService.GetListOfMeteringPoints(authorizationContext);
        
        //Get emissions in date range 
        var emissions = await _energiDataService.GetEmissionsPerHour(DateTimeUtil.ToUtcDateTime(dateFrom), DateTimeUtil.ToUtcDateTime(dateTo));

        //Get metering point time series
        var measurements = await GetTimeSeries(authorizationContext, dateFrom, dateTo, aggregation, meteringPoints);

        //Calculate total emission
        return _emissionsCalculator.CalculateEmission(emissions.Result.EmissionRecords, measurements, dateFrom, dateTo, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(AuthorizationContext authorizationContext, long dateFrom, long dateTo,
        Aggregation aggregation, IEnumerable<MeteringPoint> meteringPoints)
    {
        List<TimeSeries> timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await _dataSyncService.GetMeasurements(authorizationContext, meteringPoint.Gsrn,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime,
                DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime, aggregation);

            timeSeries.Add(new TimeSeries { Measurements = measurements, MeteringPoint = meteringPoint});
        }

        return timeSeries;
    }

    
}