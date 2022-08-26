using API.Helpers;
using API.Models;
using EnergyOriginDateTimeExtension;

namespace API.Services;

class MeasurementAggregation : IAggregator
{
    public MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, Aggregation aggregation)
    {
        var listOfMeasurements = measurements.SelectMany(
            measurement => measurement.Measurements.Select(
                reading => new AggregatedMeasurementInteral
                (
                    DateFrom: reading.DateFrom.ToDateTime(),
                    DateTo: reading.DateTo.ToDateTime(),
                    Value: reading.Quantity
                )
            )
        ).ToList();

        var groupedMeasurements = GetGroupedConsumption(aggregation, listOfMeasurements);

        var bucketMeasurements = groupedMeasurements.Select(
            group => new AggregatedMeasurement
            (
                DateFrom: group.First().DateFrom.ToUnixTime(),
                DateTo: group.Last().DateTo.ToUnixTime(),
                Value: group.Sum(it => it.Value)
            )
        ).ToList();

        return new MeasurementResponse(bucketMeasurements);
    }

    static IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> GetGroupedConsumption(Aggregation aggregation, List<AggregatedMeasurementInteral> listOfMeasurements)
    {
        var groupedMeasurements = aggregation switch
        {
            Aggregation.Year => listOfMeasurements.GroupBy(_ => _.DateFrom.Year.ToString()),
            Aggregation.Month => listOfMeasurements.GroupBy(_ => _.DateFrom.ToString("yyyy/MM")),
            Aggregation.Day => listOfMeasurements.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd")),
            Aggregation.Hour => listOfMeasurements.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.QuarterHour => listOfMeasurements.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH/mm")),
            Aggregation.Actual => listOfMeasurements.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.Total => listOfMeasurements.GroupBy(_ => "total"),
            _ => throw new ArgumentOutOfRangeException($"Invalid value {nameof(aggregation)}"),
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
