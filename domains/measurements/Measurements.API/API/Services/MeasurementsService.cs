using API.Models;
using EnergyOriginAuthorization;

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

    public async Task<MeasurementResponse> GetMeasurements(AuthorizationContext context, DateTime dateFrom,
        DateTime dateTo,
        Aggregation aggregation,
        MeterType typeOfMP)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var consumptionMeteringPoints = meteringPoints.Where(mp => mp.Type == typeOfMP);

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, consumptionMeteringPoints);

        return aggregator.CalculateAggregation(measurements, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(AuthorizationContext context, DateTime dateFrom, DateTime dateTo, IEnumerable<MeteringPoint> meteringPoints)
    {
        var timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await dataSyncService.GetMeasurements(
                context,
                meteringPoint.GSRN,
                dateFrom,
                dateTo
            );

            timeSeries.Add(new TimeSeries(meteringPoint, measurements));
        }

        return timeSeries;
    }
}
