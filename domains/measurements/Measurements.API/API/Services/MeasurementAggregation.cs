using API.Models;

namespace API.Services;

public class MeasurementAggregation : IAggregator
{
    public MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, TimeZoneInfo timeZone, Aggregation aggregation)
    {
        var list = measurements.SelectMany(
            measurement => measurement.Measurements.Select(
                reading => new AggregatedMeasurementInteral
                (
                    DateFrom: DateTimeOffset.FromUnixTimeSeconds(reading.DateFrom),
                    DateTo: DateTimeOffset.FromUnixTimeSeconds(reading.DateTo),
                    Value: reading.Quantity
                )
            )
        ).ToList();

        var groupedList = GetGroupedConsumption(aggregation, timeZone, list);

        var bucketMeasurements = groupedList.Select(
            group => new AggregatedMeasurement
            (
                DateFrom: group.First().DateFrom.ToUnixTimeSeconds(),
                DateTo: group.Last().DateTo.ToUnixTimeSeconds(),
                Value: group.Sum(it => it.Value)
            )
        ).ToList();

        return new MeasurementResponse(bucketMeasurements);
    }

    private static IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> GetGroupedConsumption(Aggregation aggregation, TimeZoneInfo timeZone, List<AggregatedMeasurementInteral> list)
    {
        var groupedMeasurements = aggregation switch
        {
            Aggregation.Year => list.GroupBy(x => Key(timeZone, "yyyy", x.DateFrom)),
            Aggregation.Month => list.GroupBy(x => Key(timeZone, "yyyy/MM", x.DateFrom)),
            Aggregation.Day => list.GroupBy(x => Key(timeZone, "yyyy/MM/dd", x.DateFrom)),
            Aggregation.Hour => list.GroupBy(x => Key(timeZone, "yyyy/MM/dd/HH", x.DateFrom)),
            Aggregation.QuarterHour => list.GroupBy(x => Key(timeZone, "yyyy/MM/dd/HH/mm", x.DateFrom)),
            Aggregation.Actual => list.GroupBy(x => Key(timeZone, "yyyy/MM/dd/HH", x.DateFrom)),
            Aggregation.Total => list.GroupBy(x => "total"),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation)),
        };
        return groupedMeasurements;
    }

    private static string Key(TimeZoneInfo timeZone, string format, DateTimeOffset date) => date.ToOffset(timeZone.GetUtcOffset(date.UtcDateTime)).ToString(format);

    private record AggregatedMeasurementInteral
    (
        DateTimeOffset DateFrom,
        DateTimeOffset DateTo,
        long Value
    );
}
