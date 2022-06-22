using API.Models;
using EnergyOriginDateTimeExtension;

namespace API.Services;

class ConsumptionCalculator : IConsumptionCalculator
{
    public MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var listOfConsumptions = new List<AggregatedMeasurementInteral>();

        listOfConsumptions.AddRange(from measurement in measurements
                                    from reading in measurement.Measurements
                                    select new AggregatedMeasurementInteral
                                    {
                                        DateFrom = reading.DateFrom.ToDateTime(),
                                        DateTo = reading.DateTo.ToDateTime(),
                                        Value = reading.Quantity
                                    });

        IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> groupedConsumptions = GetGroupedConsumption(aggregation, listOfConsumptions);

        var bucketConsumptions = (from groupedConsumption in groupedConsumptions
                                  let totalForBucket = groupedConsumption.Sum(_ => _.Value)
                                  select new AggregatedMeasurement(
                                        groupedConsumption.First().DateFrom.ToUnixTime(),
                                        groupedConsumption.Last().DateTo.ToUnixTime(),
                                        totalForBucket
                                    )).ToList();

        return new MeasurementResponse(bucketConsumptions);
    }

    static IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> GetGroupedConsumption(Aggregation aggregation, List<AggregatedMeasurementInteral> listOfConsumptions)
    {
        IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> groupedConsumptions = aggregation switch
        {
            Aggregation.Year        => listOfConsumptions.GroupBy(_ => _.DateFrom.Year.ToString()),
            Aggregation.Month       => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM")),
            Aggregation.Day         => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd")),
            Aggregation.Hour        => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.QuarterHour => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH/mm")),
            Aggregation.Actual      => listOfConsumptions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.Total       => listOfConsumptions.GroupBy(_ => "total"),
            _ => throw new ArgumentOutOfRangeException($"Invalid value {nameof(aggregation)}"),
        };
        return groupedConsumptions;
    }

    private class AggregatedMeasurementInteral
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int Value { get; set; }
    }
}
