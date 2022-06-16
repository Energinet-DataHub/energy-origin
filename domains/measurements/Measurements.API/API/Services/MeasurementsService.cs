using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class MeasurementsService : IMeasurementsService
{
    readonly IDataSyncService dataSyncService;
    readonly IConsumptionCalculator consumptionCalculator;

    public MeasurementsService(IDataSyncService dataSyncService,
            IConsumptionCalculator consumptionCalculator)
    {
        this.dataSyncService = dataSyncService;
        this.consumptionCalculator = consumptionCalculator;
    }

    public async Task<ConsumptionResponse> GetConsumption(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, aggregation, meteringPoints);

        return consumptionCalculator.CalculateConsumption(measurements, dateFrom, dateTo, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(AuthorizationContext context, long dateFrom, long dateTo,
        Aggregation aggregation, IEnumerable<MeteringPoint> meteringPoints)
    {
        List<TimeSeries> timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await dataSyncService.GetMeasurements(context, meteringPoint.GSRN,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime,
                DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime,
                aggregation);

            timeSeries.Add(new TimeSeries(meteringPoint, measurements));
        }

        return timeSeries;
    }
}
