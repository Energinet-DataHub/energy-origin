using API.Helpers;
using API.Models;
using System.Linq;
using DateTimeExtensionMethod;

namespace API.Services;

class ConsumptionCalculator : IConsumptionCalculator
{
    public ConsumptionResponse CalculateConsumption(IEnumerable<TimeSeries> measurements, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var listOfConsumptions = new List<ConsumptionInteral>();

        listOfConsumptions.AddRange(from measurement in measurements
                                    from reading in measurement.Measurements
                                    select new ConsumptionInteral
                                    {
                                        DateFrom = reading.DateFrom.FromUnixTime(),
                                        DateTo = reading.DateTo.FromUnixTime(),
                                        Value = reading.Quantity
                                    });

        IEnumerable<IGrouping<string, ConsumptionInteral>> groupedConsumptions = GetGroupedConsumption(aggregation, listOfConsumptions);

        var bucketConsumptions = (from groupedConsumption in groupedConsumptions
                                  let totalForBucket = groupedConsumption.Sum(_ => _.Value)
                                  select new Consumption(
                                        groupedConsumption.First().DateFrom.ToUnixTime(),
                                        groupedConsumption.Last().DateTo.ToUnixTime(),
                                        totalForBucket
                                    )).ToList();

        return new ConsumptionResponse(bucketConsumptions);
    }

    static IEnumerable<IGrouping<string, ConsumptionInteral>> GetGroupedConsumption(Aggregation aggregation, List<ConsumptionInteral> listOfConsumptions)
    {
        IEnumerable<IGrouping<string, ConsumptionInteral>> groupedConsumptions = aggregation switch
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
    private class ConsumptionInteral
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public float Value { get; set; }
    }
}
