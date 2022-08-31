using API.Shared.DataSync;
using API.Shared.DataSync.Models;
using EnergyOriginAuthorization;

namespace API.Extensions;

public record TimeSeries
{
    public MeteringPoint MeteringPoint { get; }

    public IEnumerable<Measurement> Measurements { get; }

    public TimeSeries(MeteringPoint meteringPoint, IEnumerable<Measurement> measurements)
    {
        MeteringPoint = meteringPoint;
        Measurements = measurements;
    }
}

public static class TimeSeriesExtension
{
    public static async Task<IEnumerable<TimeSeries>> GetTimeSeries(
        this IDataSyncService dataSyncService,
        AuthorizationContext context,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        IEnumerable<MeteringPoint> meteringPoints)
    {
        var timeSeries = new List<TimeSeries>();
        foreach (var meteringPoint in meteringPoints)
        {
            var measurements = await dataSyncService.GetMeasurements(context, meteringPoint.GSRN, dateFrom, dateTo);

            timeSeries.Add(new TimeSeries(meteringPoint, measurements));
        }

        return timeSeries;
    }
}
