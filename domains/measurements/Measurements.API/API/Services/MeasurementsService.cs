using API.Models;
using EnergyOriginAuthorization;
using Serilog;

namespace API.Services;

public class MeasurementsService : IMeasurementsService
{
    readonly IDataSyncService dataSyncService;
    readonly IAggregator aggregator;

    public MeasurementsService(IDataSyncService dataSyncService, IAggregator aggregator)
    {
        this.dataSyncService = dataSyncService;
        this.aggregator = aggregator;
    }

    public async Task<MeasurementResponse> GetMeasurements(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var consumptionMeteringPoints = meteringPoints.Where(mp => mp.Type == MeterType.Consumption);

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, consumptionMeteringPoints);

        return aggregator.CalculateAggregation(measurements, dateFrom, dateTo, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(AuthorizationContext context, long dateFrom, long dateTo, IEnumerable<MeteringPoint> meteringPoints)
    {
        var timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await dataSyncService.GetMeasurements(
                context,
                meteringPoint.GSRN,
                DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime,
                DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime
            );

            timeSeries.Add(new TimeSeries(meteringPoint, measurements));
        }

        return timeSeries;
    }
}
