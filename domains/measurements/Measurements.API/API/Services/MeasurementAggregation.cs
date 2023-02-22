using API.Models;
using EnergyOriginDateTimeExtension;

namespace API.Services;

public class MeasurementAggregation : IAggregator
{
    public MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, TimeZoneInfo timeZone, Aggregation aggregation)
    {
        var list = measurements.SelectMany(
            measurement => measurement.Measurements.Select(
                reading => new AggregatedMeasurementInteral
                (
                    DateFrom: reading.DateFrom.ToDateTime(),
                    DateTo: reading.DateTo.ToDateTime(),
                    Value: reading.Quantity
                )
            )
        ).ToList();

        var groupedList = GetGroupedConsumption(aggregation, list);

        var bucketMeasurements = groupedList.Select(
            group => new AggregatedMeasurement
            (
                DateFrom: group.First().DateFrom.ToUnixTime(),
                DateTo: group.Last().DateTo.ToUnixTime(),
                Value: group.Sum(it => it.Value)
            )
        ).ToList();

        return new MeasurementResponse(bucketMeasurements);
    }

    private static IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> GetGroupedConsumption(Aggregation aggregation, List<AggregatedMeasurementInteral> list)
    {
        var groupedMeasurements = aggregation switch
        {
            Aggregation.Year => list.GroupBy(x => x.DateFrom.Year.ToString()),
            Aggregation.Month => list.GroupBy(x => x.DateFrom.ToString("yyyy/MM")),
            Aggregation.Day => list.GroupBy(x => x.DateFrom.ToString("yyyy/MM/dd")),
            Aggregation.Hour => list.GroupBy(x => x.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.QuarterHour => list.GroupBy(x => x.DateFrom.ToString("yyyy/MM/dd/HH/mm")),
            Aggregation.Actual => list.GroupBy(x => x.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.Total => list.GroupBy(x => "total"),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation)),
        };
        return groupedMeasurements;
    }

    private record AggregatedMeasurementInteral
    (
        DateTime DateFrom,
        DateTime DateTo,
        long Value
    );
}
