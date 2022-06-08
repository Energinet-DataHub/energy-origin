using API.Helpers;
using API.Models;

namespace API.Services
{
    public class SourcesCalculator : ISourcesCalculator
    {
        readonly IList<string> renewableSources = Configuration.GetRenewableSources();

        public EnergySourceResponse CalculateSourceEmissions(
            IEnumerable<TimeSeries> timeSeries,
            List<Record> declarationRecords,
            Aggregation aggregation)
        {
            var groupedDeclarations = GetGroupedDeclarations(aggregation, declarationRecords);
            var consumptionResults = new Dictionary<string, Dictionary<string, ConsumptionShare>>();

            foreach (var measurements in timeSeries)
            {
                foreach (var measurement in measurements.Measurements)
                {
                    var utcDateTime = GetDateAsString(measurement.DateFrom.ToUtcDateTime(), aggregation);
                    var gridArea = measurements.MeteringPoint.GridArea;
                    var totalShares = groupedDeclarations[utcDateTime + gridArea];

                    CalculateConsumptionShare(consumptionResults, utcDateTime, totalShares, measurement);
                }
            }
            var result = CalculateSourceEmissionPercentage(timeSeries, aggregation, consumptionResults);

            return result;
        }

        void CalculateConsumptionShare(
            Dictionary<string, Dictionary<string, ConsumptionShare>> consumptionResults,
            string utcDateTime,
            IEnumerable<Record> totalShares,
            Measurement measurement)
        {
            if (!consumptionResults.TryGetValue(utcDateTime, out var shares))
            {
                shares = new Dictionary<string, ConsumptionShare>();
                consumptionResults.Add(utcDateTime, shares);
            }

            foreach (var totalShare in totalShares.Where(a => a.HourUTC.ToUnixTime() == measurement.DateFrom))
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
                if (measurement.DateFrom < share.DateFrom)
                {
                    share.DateFrom = measurement.DateFrom;
                }

                if (measurement.DateTo > share.DateTo)
                {
                    share.DateTo = measurement.DateTo;
                }
            }
        }

        EnergySourceResponse CalculateSourceEmissionPercentage(
            IEnumerable<TimeSeries> timeSeries,
            Aggregation aggregation,
            Dictionary<string, Dictionary<string, ConsumptionShare>> consumptionResults)
        {
            var result = new EnergySourceResponse(new List<EnergySourceDeclaration>());
            var groupedMeasurements = GetGroupedMeasurements(aggregation, timeSeries);

            foreach (var consumptionResult in consumptionResults)
            {
                var matchingConsumptionSum = groupedMeasurements[consumptionResult.Key].Sum(_ => _.Quantity);

                var sources = consumptionResult.Value.ToDictionary(a =>
                    a.Key, b => (float)Math.Round(b.Value.Value / (matchingConsumptionSum * 100), 5));

                result.EnergySources.Add(new EnergySourceDeclaration
                (
                    consumptionResult.Value.Min(a => a.Value.DateFrom),
                    consumptionResult.Value.Max(a => a.Value.DateTo),
                    CalculateRenewable(sources),
                    sources
                ));
            }

            return result;
        }

        float CalculateRenewable(IDictionary<string, float> groupValues) =>
            groupValues.Where(a => renewableSources.Contains(a.Key)).Sum(a => a.Value);

        ILookup<string, Measurement> GetGroupedMeasurements(Aggregation aggregation, IEnumerable<TimeSeries> timeSeries)
        {
            if (aggregation == Aggregation.Total)
                return timeSeries.SelectMany(a => a.Measurements).ToLookup(x => "total");

            return timeSeries.SelectMany(y => y.Measurements)
                .ToLookup(x => GetDateAsString(x.DateFrom.ToUtcDateTime(), aggregation));
        }

        ILookup<string, Record> GetGroupedDeclarations(Aggregation aggregation, List<Record> declaration)
        {
            if (aggregation == Aggregation.Total)
                return declaration.ToLookup(a => "total" + a.PriceArea);

            return declaration.ToLookup(a => GetDateAsString(a.HourUTC, aggregation) + a.PriceArea);
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