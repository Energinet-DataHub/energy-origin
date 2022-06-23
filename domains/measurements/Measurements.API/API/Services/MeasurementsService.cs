using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class MeasurementsService : IMeasurementsService
{
    readonly IDataSyncService dataSyncService;
    readonly IConsumptionAggregator aggregateMeasurements;

    public MeasurementsService(IDataSyncService dataSyncService,
            IConsumptionAggregator AggregateMeasurements)
    {
        this.dataSyncService = dataSyncService;
        this.aggregateMeasurements = AggregateMeasurements;
    }

    public async Task<MeasurementResponse> GetConsumption(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, aggregation, meteringPoints.Where(mp => mp.Type == MeterType.Consumption));

        return aggregateMeasurements.CalculateAggregation(measurements, dateFrom, dateTo, aggregation);
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
}
