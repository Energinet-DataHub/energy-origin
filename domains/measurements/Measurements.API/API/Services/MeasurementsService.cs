using API.OldModels;
using API.OldModels.Response;
using EnergyOriginAuthorization;

namespace API.Services;

public class MeasurementsService : IMeasurementsService
{
    private readonly IDataSyncService dataSyncService;
    private readonly IAggregator aggregator;

    public MeasurementsService(IDataSyncService dataSyncService, IAggregator aggregator)
    {
        this.dataSyncService = dataSyncService;
        this.aggregator = aggregator;
    }

    public async Task<MeasurementResponse> GetMeasurements(
        AuthorizationContext context,
        TimeZoneInfo timeZone,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        Aggregation aggregation,
        MeterType typeOfMP)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(context);

        var consumptionMeteringPoints = meteringPoints.Where(mp => mp.Type == typeOfMP);

        var measurements = await GetTimeSeries(context, dateFrom, dateTo, consumptionMeteringPoints);

        return aggregator.CalculateAggregation(measurements, timeZone, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, IEnumerable<MeteringPoint> meteringPoints)
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
