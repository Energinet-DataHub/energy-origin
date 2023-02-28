using API.Helpers;
using API.Models;

namespace API.Services
{
    public class SourcesCalculator : ISourcesCalculator
    {
        private readonly IList<string> renewableSources = Configuration.GetRenewableSources();
        private readonly decimal wasteRenewableShare = Configuration.GetWasteRenewableShare();
        private const string waste = "waste";
        private const string total = "total";

        public EnergySourceResponse CalculateSourceEmissions(
            IEnumerable<TimeSeries> timeSeries,
            IEnumerable<MixRecord> records,
            TimeZoneInfo timeZone,
            Aggregation aggregation)
        {
            var result = new EnergySourceResponse(new List<EnergySourceDeclaration>());

            //Get dictionary of measurement values using aggregated date string as key.
            var measurementGroups =
                (from singleTimeSeries in timeSeries
                 from measurement in singleTimeSeries.Measurements
                 select new
                 {
                     singleTimeSeries.MeteringPoint.GridArea,
                     measurement.DateFrom,
                     measurement.DateTo,
                     measurement.Quantity
                 })
                .GroupBy(x => GetAggregationDateString(DateTimeOffset.FromUnixTimeSeconds(x.DateFrom), timeZone, aggregation));

            //Go through each period (aggregated date string).
            foreach (var measurementGroup in measurementGroups)
            {
                var totalConsumption = measurementGroup.Sum(x => x.Quantity);

                //Get a summed share value, date interval and percentage of total for each production type.
                var consumptionShares =
                    from measurement in measurementGroup
                    join declaration in records
                        on new { measurement.GridArea, DateFrom = DateTimeOffset.FromUnixTimeSeconds(measurement.DateFrom) }
                        equals new { declaration.GridArea, DateFrom = (DateTimeOffset)declaration.HourUTC }
                    group new { measurement, declaration } by declaration.ProductionType into productionTypeGroup
                    let shareValue = productionTypeGroup.Sum(x => x.declaration.ShareTotal * x.measurement.Quantity)
                    select new
                    {
                        productionType = productionTypeGroup.Key,
                        shareValue,
                        dateFrom = productionTypeGroup.Min(x => x.measurement.DateFrom),
                        dateTo = productionTypeGroup.Max(x => x.measurement.DateTo),
                        percentageOfTotal = Math.Round(shareValue / totalConsumption / 100, Configuration.DecimalPrecision)
                    };

                //Get period in unix timestamps for entire aggregation.
                var dateFrom = consumptionShares.Min(x => x.dateFrom);
                var dateTo = consumptionShares.Max(x => x.dateTo);

                //Create production type percentage dictionary and calculate renewable percentage for period.
                var sourcesWithPercentage = consumptionShares.ToDictionary(x => x.productionType, b => b.percentageOfTotal);
                var renewablePercentage = sourcesWithPercentage
                    .Where(a => renewableSources.Contains(a.Key))
                    .Sum(x => x.Value * (x.Key == waste ? wasteRenewableShare : 1));

                //Add to results
                result.EnergySources.Add(new EnergySourceDeclaration(dateFrom, dateTo, renewablePercentage, sourcesWithPercentage));
            }

            return result;
        }

        //Translates a date into an aggregated date string.
        //This creates a period bucket/bin spanning the aggregation amount.
        private static string GetAggregationDateString(DateTimeOffset date, TimeZoneInfo timeZone, Aggregation aggregation) => aggregation switch
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
}
