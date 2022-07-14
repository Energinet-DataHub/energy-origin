using API.Models;
using EnergyOriginDateTimeExtension;

namespace API.Services;

class MeasurementAggregation : IAggregator
{
    public MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var listOfMeasurements = measurements.SelectMany(
            measurement => measurement.Measurements.Select(
                reading => new AggregatedMeasurementInteral(
                    reading.DateFrom.ToDateTime(),
                    reading.DateTo.ToDateTime(),
                    reading.Quantity
                )
            )
        ).ToList();

        IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> groupedMeasurements = GetGroupedConsumption(aggregation, listOfMeasurements);

        var bucketMeasurements = groupedMeasurements.Select(
            group => new AggregatedMeasurement(
                group.First().DateFrom.ToUnixTime(),
                group.Last().DateTo.ToUnixTime(),
                group.Sum(it => it.Value)
            )
        ).ToList();

        return new MeasurementResponse(bucketMeasurements);
    }

    static IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> GetGroupedConsumption(Aggregation aggregation, List<AggregatedMeasurementInteral> listOfMeasurements)
    {
        IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> groupedMeasurements = aggregation switch
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

    private class AggregatedMeasurementInteral
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int Value { get; set; }

        public AggregatedMeasurementInteral(DateTime dateFrom, DateTime dateTo, int value)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Value = value;
        }
    }
}
