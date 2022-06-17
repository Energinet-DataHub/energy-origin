using API.Helpers;
using API.Models;
using EnergyOriginDateTimeExtension;

namespace API.Services
{
    public class SourcesCalculator : ISourcesCalculator
    {
        readonly IList<string> renewableSources = Configuration.GetRenewableSources();
        readonly float wasteRenewableShare = Configuration.GetWasteRenewableShare();
        private const string waste = "waste";
        private const string total = "total";

        public EnergySourceResponse CalculateSourceEmissions(
            IEnumerable<TimeSeries> timeSeries,
            List<Record> records,
            Aggregation aggregation)
        {
            var groupedDeclarations = GetGroupedDeclarations(aggregation, records);
            var consumptionResults = new Dictionary<string, Dictionary<string, ConsumptionShare>>();

            foreach (var measurements in timeSeries)
            {
                foreach (var measurement in measurements.Measurements)
                {
                    var dateTime = GetDateAsString(measurement.DateFrom.ToDateTime(), aggregation);
                    var gridArea = measurements.MeteringPoint.GridArea;
                    var totalShares = groupedDeclarations[dateTime + gridArea];

                    CalculateConsumptionShareByReference(consumptionResults, dateTime, totalShares, measurement);
                }
            }
            var result = CalculateSourceEmissionPercentage(timeSeries, aggregation, consumptionResults);

            return result;
        }

        void CalculateConsumptionShareByReference(
            Dictionary<string, Dictionary<string, ConsumptionShare>> consumptionResults,
            string dateTime,
            IEnumerable<Record> totalShares,
            Measurement measurement)
        {
            if (!consumptionResults.TryGetValue(dateTime, out var shares))
            {
                shares = new Dictionary<string, ConsumptionShare>();
                consumptionResults.Add(dateTime, shares);
            }

            foreach (var totalShare in totalShares.Where(a => a.HourUTC.ToUnixTime() == measurement.DateFrom))
            {
                if (!shares.TryGetValue(totalShare.ProductionType, out var share))
                {
                    share = new ConsumptionShare
                    (
                        0,
                        measurement.DateFrom,
                        measurement.DateTo,
                        totalShare.ProductionType
                    );
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
                    a.Key, b => (float)Math.Round((b.Value.Value / matchingConsumptionSum / 100), 5));

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
             groupValues.Where(a => renewableSources.Contains(a.Key)).
                    Sum(a => a.Value * (a.Key == waste ? wasteRenewableShare : 1));

        ILookup<string, Measurement> GetGroupedMeasurements(Aggregation aggregation, IEnumerable<TimeSeries> timeSeries)
        {
            if (aggregation == Aggregation.Total)
                return timeSeries.SelectMany(a => a.Measurements).ToLookup(x => total);

            return timeSeries.SelectMany(y => y.Measurements)
                .ToLookup(x => GetDateAsString(x.DateFrom.ToDateTime(), aggregation));
        }

        ILookup<string, Record> GetGroupedDeclarations(Aggregation aggregation, List<Record> declaration)
        {
            if (aggregation == Aggregation.Total)
                return declaration.ToLookup(a => total + a.PriceArea);

            return declaration.ToLookup(a => GetDateAsString(a.HourUTC, aggregation) + a.PriceArea);
        }

        string GetDateAsString(DateTime date, Aggregation aggregation)
        {
            return aggregation switch
            {
                Aggregation.Year => date.ToString("yyyy"),
                Aggregation.Month => date.ToString("yyyy/MM"),
                Aggregation.Day => date.ToString("yyyy/MM/dd"),
                Aggregation.Hour => date.ToString("yyyy/MM/dd/HH"),
                Aggregation.QuarterHour => date.ToString("yyyy/MM/dd/HH/mm"),
                Aggregation.Actual => date.ToString("yyyy/MM/dd/HH"),
                Aggregation.Total => total,
                _ => throw new ArgumentOutOfRangeException(nameof(aggregation), aggregation, null),
            };
        }

        class ConsumptionShare
        {
            public float Value { get; set; }
            public long DateFrom { get; set; }
            public long DateTo { get; set; }
            public string ProductionType { get; }

            public ConsumptionShare(float value, long dateFrom, long dateTo, string productionType)
            {
                Value = value;
                DateFrom = dateFrom;
                DateTo = dateTo;
                ProductionType = productionType;
            }
        }
    }
}
