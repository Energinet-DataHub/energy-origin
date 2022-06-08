using System.Runtime.InteropServices.ComTypes;
using API.Helpers;
using API.Models;


namespace API.Services
{
    public class SourcesCalculator : ISourcesCalculator
    {
        private IList<string> RenewableSources = Configuration.GetRenewableSources();


        public EnergySourceResponse CalculateSourceEmissions(IEnumerable<TimeSeries> timeSeries, DeclarationProduction declaration,
            long dateFrom, long dateTo, Aggregation aggregation)
        {
            EnergySourceResponse result = new EnergySourceResponse(new List<EnergySourceDeclaration>());

            IEnumerable<IGrouping<string, Record>> groupedDeclarations = GetGroupedDeclarations(aggregation, declaration.Result.Records);
            var consumptionResults = new Dictionary<string, Dictionary<string, ConsumptionShare>>();

            foreach (var measurements in timeSeries)
            {
                foreach (var measurement in measurements.Measurements)
                {
                    var utcDateTime = GetDateAsString(measurement.DateFrom.ToUtcDateTime(), aggregation);
                    var gridArea = measurements.MeteringPoint.GridArea;
                    var totalShares = groupedDeclarations.Single(_ =>
                        _.Key == utcDateTime + gridArea
                    );

                    CalculateConsumptionShare(consumptionResults, utcDateTime, totalShares, measurement);
                }
            }
            CalculateSourceEmissionPercentage(timeSeries, aggregation, consumptionResults, result);

            return result;

        }

        static void CalculateConsumptionShare(Dictionary<string, Dictionary<string, ConsumptionShare>> consumptionResults, string utcDateTime, IGrouping<string, Record> totalShares,
            Measurement measurement)
        {
            if (!consumptionResults.TryGetValue(utcDateTime, out var shares))
            {
                shares = new Dictionary<string, ConsumptionShare>();
                consumptionResults.Add(utcDateTime, shares);
            }

            foreach (var totalShare in totalShares.Where(_ => _.HourUTC.ToUnixTime() == measurement.DateFrom))
            {
                if (!shares.TryGetValue(totalShare.ProductionType, out var share))
                {
                    share = new ConsumptionShare
                    {
                        Value = 0,
                        DateFrom = measurement.DateFrom,
                        DateTo = measurement.DateTo,
                        ProductionType = totalShare.ProductionType
                    };
                    shares.Add(totalShare.ProductionType, share);
                }

                share.Value += (float)totalShare.ShareTotal * measurement.Quantity;
            }
        }

        void CalculateSourceEmissionPercentage(IEnumerable<TimeSeries> timeSeries, Aggregation aggregation, Dictionary<string, Dictionary<string, ConsumptionShare>> consumptionResults,
            EnergySourceResponse result)
        {
            IEnumerable<IGrouping<string, Measurement>> groupedMeasurements = GetGroupedMeasurements(aggregation, timeSeries);

            foreach (var consumptionResult in consumptionResults)
            {
                var matchingConsumptionSum =
                    groupedMeasurements.Single(_ => _.Key == consumptionResult.Key).Sum(_ => _.Quantity);

                var sources =
                    consumptionResult.Value.ToDictionary(_ => _.Key, _ => _.Value.Value / (matchingConsumptionSum * 100));

                result.EnergySources.Add(new EnergySourceDeclaration
                (
                    consumptionResult.Value.Min(_ => _.Value.DateFrom),
                    consumptionResult.Value.Max(_ => _.Value.DateTo),
                    CalculateRenewable(sources),
                    sources
                ));
            }
        }

        float CalculateRenewable(IDictionary<string, float> groupValues)
        {
            return groupValues.Where(_ => RenewableSources.Contains(_.Key)).Sum(_ => _.Value);
        }

        IEnumerable<IGrouping<string, Measurement>> GetGroupedMeasurements(Aggregation aggregation, IEnumerable<TimeSeries> timeSeries)
        {
            if (aggregation == Aggregation.Total)
            {
                return timeSeries.SelectMany(_ => _.Measurements).GroupBy(x => "total");
            }

            return timeSeries.SelectMany(y => y.Measurements)
                .GroupBy(x => GetDateAsString(x.DateFrom.ToUtcDateTime(), aggregation));
        }

        IEnumerable<IGrouping<string, Record>> GetGroupedDeclarations(Aggregation aggregation, List<Record> declaration)
        {
            if (aggregation == Aggregation.Total)
            {
                return declaration.GroupBy(_ => "total" + _.PriceArea);
            }

            return declaration.GroupBy(_ => GetDateAsString(_.HourUTC, aggregation) + _.PriceArea);

        }

        string GetDateAsString(DateTime date, Aggregation aggregation)
        {
            switch (aggregation)
            {
                case Aggregation.Year:
                    return date.ToString("yyyy");
                case Aggregation.Month:
                    return date.ToString("yyyy/MM");
                case Aggregation.Day:
                    return date.ToString("yyyy/MM/dd");
                case Aggregation.Hour:
                    return date.ToString("yyyy/MM/dd/HH");
                case Aggregation.QuarterHour:
                    return date.ToString("yyyy/MM/dd/HH/mm");
                case Aggregation.Actual:
                    return date.ToString("yyyy/MM/dd/HH");
                case Aggregation.Total:
                    return "total";
                default:
                    throw new ArgumentOutOfRangeException(nameof(aggregation), aggregation, null);
            }


        }

        class ConsumptionShare
        {
            public float Value { get; set; }

            public long DateFrom { get; set; }
            public long DateTo { get; set; }

            public string? ProductionType { get; set; }

        }
    }
}
