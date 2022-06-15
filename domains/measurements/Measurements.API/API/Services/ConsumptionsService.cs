using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class MeasurementsService : IMeasurementsService
{
    readonly IDataSyncService dataSyncService;
    readonly IConsumptionsCalculator consumptionsCalculator;

    public MeasurementsService(IDataSyncService dataSyncService,
            IConsumptionsCalculator consumptionsCalculator)
    {
        this.dataSyncService = dataSyncService;
        this.consumptionsCalculator = consumptionsCalculator;
    }

    public async Task<ConsumptionsResponse> GetConsumptions(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, aggregation, meteringPoints);

        return consumptionsCalculator.CalculateConsumptions(measurements, dateFrom, dateTo, aggregation);
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