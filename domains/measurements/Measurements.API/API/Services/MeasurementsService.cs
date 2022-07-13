using API.Models;
using EnergyOriginAuthorization;
using Serilog;

namespace API.Services;

public class MeasurementsService : IMeasurementsService
{
    readonly IDataSyncService dataSyncService;
    readonly IConsumptionAggregator aggregator;

    public MeasurementsService(IDataSyncService dataSyncService, IConsumptionAggregator aggregator)
    {
        this.dataSyncService = dataSyncService;
        this.aggregator = aggregator;
    }

    public async Task<MeasurementResponse> GetConsumption(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var consumptionMeteringPoints = meteringPoints.Where(mp => mp.Type == MeterType.Consumption);
        Log.Information("Hello, {@consumptionMeteringPoints}, at date range {dateFrom}, {dateTo}",
          new
          {
              Name = "consumptionMeteringPoints",
              consumptionMeteringPoints = new[] { consumptionMeteringPoints }
          },
          dateFrom,
          dateTo);
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
