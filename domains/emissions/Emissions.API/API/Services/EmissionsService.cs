using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class EmissionsService : IEmissionsService
{
    readonly IDataSyncService _dataSyncService;
    private readonly IEnergiDataService _energiDataService;

    public EmissionsService(IDataSyncService dataSyncService, IEnergiDataService energiDataService)
    {
        _dataSyncService = dataSyncService;
        _energiDataService = energiDataService;
    }

    public async Task<IEnumerable<GetEmissionsResponse>> GetEmissions(AuthorizationContext authorizationContext,
        long dateFrom, long dateTo, Aggregation aggregation)
    {
        //Get list of metering points
        var meteringPoints = await _dataSyncService.GetListOfMeteringPoints(authorizationContext);
        
        //Get emissions in date range 
        var emissions = await _energiDataService.GetEmissions(DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime, DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime, "DK1");

        //Get metering point time series
        Dictionary<long, IEnumerable<Measurement>> measurements = new Dictionary<long, IEnumerable<Measurement>>();
        foreach (var meteringPoint in meteringPoints)
        {
            var timeSeries = await _dataSyncService.GetMeasurements(authorizationContext, meteringPoint,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime,
                DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime, aggregation);
            
            measurements.Add(meteringPoint, timeSeries);
        }

        return CalculateTotalEmission(emissions, measurements);

    }
}