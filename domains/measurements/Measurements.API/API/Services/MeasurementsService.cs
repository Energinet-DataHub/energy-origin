using API.Models;
using System.Net.Http.Headers;

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
        AuthenticationHeaderValue bearerToken,
        TimeZoneInfo timeZone,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        Aggregation aggregation,
        MeterType typeOfMP)
    {
        var meteringPoints = await dataSyncService.GetListOfMeteringPoints(bearerToken);

        var consumptionMeteringPoints = meteringPoints.Where(mp => mp.Type == typeOfMP);

        var measurements = await GetTimeSeries(bearerToken, dateFrom, dateTo, consumptionMeteringPoints);

        return aggregator.CalculateAggregation(measurements, timeZone, aggregation);
    }

    public async Task<IEnumerable<TimeSeries>> GetTimeSeries(AuthenticationHeaderValue bearerToken, DateTimeOffset dateFrom, DateTimeOffset dateTo, IEnumerable<MeteringPoint> meteringPoints)
    {
        var timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await dataSyncService.GetMeasurements(
                bearerToken,
                meteringPoint.GSRN,
                dateFrom,
                dateTo
            );

            timeSeries.Add(new TimeSeries(meteringPoint, measurements));
        }

        return timeSeries;
    }
}
