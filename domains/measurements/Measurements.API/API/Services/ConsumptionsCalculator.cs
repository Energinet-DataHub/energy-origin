using API.Helpers;
using API.Models;

namespace API.Services;

class ConsumptionsCalculator : IConsumptionsCalculator
{
    public ConsumptionsResponse CalculateConsumptions(IEnumerable<TimeSeries> measurements, long dateFrom, long dateTo, Aggregation aggregation)
    {
        var listOfConsumptions = new List<ConsumptionsInteral>();

        foreach (var measurement in measurements)
        {
            foreach (var reading in measurement.Measurements)
            {
                listOfConsumptions.Add(new ConsumptionsInteral
                {
                    DateFrom = reading.DateFrom.ToUtcDateTime(),
                    DateTo = reading.DateTo.ToUtcDateTime(),
                    Value = reading.Quantity
                });
            }
        }

        IEnumerable<IGrouping<string, ConsumptionsInteral>> groupedConsumptions = GetGroupedConsumptions(aggregation, listOfConsumptions);

        var bucketConsumptions = new List<Consumptions>();
        foreach (var groupedConsumption in groupedConsumptions)
        {
            var totalForBucket = groupedConsumption.Sum(_ => _.Value);
            bucketConsumptions.Add(new Consumptions(
                groupedConsumption.First().DateFrom.ToUnixTime(),
                groupedConsumption.Last().DateTo.ToUnixTime(),
                totalForBucket
            ));
        }

        var consumptionsResult = new ConsumptionsResponse(bucketConsumptions);

        return consumptionsResult;
    }

    static IEnumerable<IGrouping<string, ConsumptionsInteral>> GetGroupedConsumptions(Aggregation aggregation, List<ConsumptionsInteral> listOfConsumptions)
    {
        IEnumerable<IGrouping<string, ConsumptionsInteral>> groupedConsumptions = aggregation switch
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
}

internal class ConsumptionsInteral
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public float Value { get; set; }
}
