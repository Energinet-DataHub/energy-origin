using API.Models;
using EnergyOriginDateTimeExtension;

namespace API.Services;

class ConsumptionAggregation : IConsumptionAggregator
{
    public MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var listOfConsumptions = measurements.SelectMany(
            measurement => measurement.Measurements.Select(
                reading => new AggregatedMeasurementInteral(
                    reading.DateFrom.ToDateTime(),
                    reading.DateTo.ToDateTime(),
                    reading.Quantity / 1000
                )
            )
        ).ToList();

        IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> groupedConsumptions = GetGroupedConsumption(aggregation, listOfConsumptions);

        var bucketConsumptions = groupedConsumptions.Select(
            group => new AggregatedMeasurement(
                group.First().DateFrom.ToUnixTime(),
                group.Last().DateTo.ToUnixTime(),
                group.Sum(it => it.Value)
            )
        ).ToList();

        return new MeasurementResponse(bucketConsumptions);
    }

    static IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> GetGroupedConsumption(Aggregation aggregation, List<AggregatedMeasurementInteral> listOfConsumptions)
    {
        IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> groupedConsumptions = aggregation switch
        {
            Aggregation.Year => listOfConsumptions.GroupBy(_ => _.DateFrom.Year.ToString()),
            Aggregation.Month => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM")),
            Aggregation.Day => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd")),
            Aggregation.Hour => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.QuarterHour => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH/mm")),
            Aggregation.Actual => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.Total => listOfConsumptions.GroupBy(_ => "total"),
            _ => throw new ArgumentOutOfRangeException($"Invalid value {nameof(aggregation)}"),
        };
        return groupedConsumptions;
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
