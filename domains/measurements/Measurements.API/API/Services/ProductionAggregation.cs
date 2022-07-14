using API.Models;
using EnergyOriginDateTimeExtension;

namespace API.Services
{
    public class ProductionAggregation : IProductionAggregator
    {
        public MeasurementResponse CalculateAggregation(IEnumerable<TimeSeries> measurements, long dateFrom, long dateTo, Aggregation aggregation)
        {
            var listOfProductions = measurements.SelectMany(
                measurement => measurement.Measurements.Select(
                    reading => new AggregatedMeasurementInteral(
                        reading.DateFrom.ToDateTime(),
                        reading.DateTo.ToDateTime(),
                        reading.Quantity
                    )
                )
            ).ToList();

            IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> groupedProductions = GetGroupedProductions(aggregation, listOfProductions);

            var bucketProductions = groupedProductions.Select(
                group => new AggregatedMeasurement(
                    group.First().DateFrom.ToUnixTime(),
                    group.Last().DateTo.ToUnixTime(),
                    group.Sum(it => it.Value)
                )
            ).ToList();

            return new MeasurementResponse(bucketProductions);
        }

        static IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> GetGroupedProductions(Aggregation aggregation, List<AggregatedMeasurementInteral> listOfProductions)
        {
            IEnumerable<IGrouping<string, AggregatedMeasurementInteral>> groupedProductions = aggregation switch
            {
                Aggregation.Year => listOfProductions.GroupBy(_ => _.DateFrom.Year.ToString()),
                Aggregation.Month => listOfProductions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM")),
                Aggregation.Day => listOfProductions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd")),
                Aggregation.Hour => listOfProductions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH")),
                Aggregation.QuarterHour => listOfProductions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH/mm")),
                Aggregation.Actual => listOfProductions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH")),
                Aggregation.Total => listOfProductions.GroupBy(_ => "total"),
                _ => throw new ArgumentOutOfRangeException($"Invalid value {nameof(aggregation)}"),
            };
            return groupedProductions;
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
}
