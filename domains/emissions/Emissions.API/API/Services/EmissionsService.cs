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

    public async Task<Emissions> GetEmissions(AuthorizationContext authorizationContext,
        long dateFrom, long dateTo, Aggregation aggregation)
    {
        //Get list of metering points
        var meteringPoints = await _dataSyncService.GetListOfMeteringPoints(authorizationContext);
        
        //Get emissions in date range 
        var emissions = await _energiDataService.GetEmissions(DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime, DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime);

        //Get metering point time series
        List<Tuple<MeteringPoint, IEnumerable<Measurement>>> measurements = new List<Tuple<MeteringPoint, IEnumerable<Measurement>>>();
        foreach (var meteringPoint in meteringPoints)
        {
            var timeSeries = await _dataSyncService.GetMeasurements(authorizationContext, meteringPoint.Gsrn,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime,
                DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime, aggregation);
            
            measurements.Add(new Tuple<MeteringPoint, IEnumerable<Measurement>>(meteringPoint, timeSeries));
        }

        return CalculateTotalEmission(emissions.Result.EmissionRecords, measurements, dateFrom, dateTo);

    }

    internal virtual Emissions CalculateTotalEmission(List<EmissionRecord> emissions,
        List<Tuple<MeteringPoint, IEnumerable<Measurement>>> measurements, long dateFrom, long dateTo)
    {
        float totalEmission = 0;
        uint totalConsumption = 0;
        float relative = 0;
        
        foreach (var measurementEntry in measurements)
        {
            var timeSeries = measurementEntry.Item2;
            totalConsumption += (uint)timeSeries.Sum(_ => _.Quantity);
            foreach (var emission in emissions)
            {
                var co2 = emission.CO2PerkWh * (timeSeries.First(_ => emission.GridArea ==  measurementEntry.Item1.GridArea && DateTimeOffset.FromUnixTimeSeconds(_.DateFrom) == emission.HourUTC).Quantity / 1000);
                totalEmission += co2;
            }
             
        }

        relative = totalEmission / totalConsumption;
        return new Emissions
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Total = new Total {CO2 = totalEmission},
            Relative = new Relative {CO2 = relative},
        };
    }
}