using API.Helpers;
using API.Models;
using EnergyOriginDateTimeExtension;

namespace API.Services
{
    public class SourcesCalculator : ISourcesCalculator
    {
        readonly IList<string> renewableSources = Configuration.GetRenewableSources();
        readonly decimal wasteRenewableShare = Configuration.GetWasteRenewableShare();
        const string waste = "waste";
        const string total = "total";

        public EnergySourceResponse CalculateSourceEmissions(
            IEnumerable<TimeSeries> timeSeries,
            IEnumerable<MixRecord> records,
            Aggregation aggregation)
        {
            var result = new EnergySourceResponse(new List<EnergySourceDeclaration>());

            //Get dictionary of measurement values using aggregated date string as key.
            var measurementGroups = 
                (from singleTimeSeries in timeSeries
                from measurement in singleTimeSeries.Measurements
                select new { 
                    singleTimeSeries.MeteringPoint.GridArea, 
                    measurement.DateFrom, 
                    measurement.DateTo, 
                    measurement.Quantity })
                .GroupBy(a => GetAggregationDateString(a.DateFrom.ToDateTime(), aggregation));

            //Go through each period (aggregated date string).
            foreach (var measurementGroup in measurementGroups)
            {
                var totalConsumption = measurementGroup.Sum(a => a.Quantity);

                //Get a summed share value, date interval and percentage of total for each production type.
                var consumptionShares =
                    from measurement in measurementGroup
                    join declaration in records
                        on new { measurement.GridArea, DateFrom = measurement.DateFrom.ToDateTime() }
                        equals new { declaration.GridArea, DateFrom = declaration.HourUTC }                    
                    group new { measurement, declaration } by declaration.ProductionType into productionTypeGroup
                    let shareValue = productionTypeGroup.Sum(a => a.declaration.ShareTotal * a.measurement.Quantity)
                    select new
                    {
                        productionType = productionTypeGroup.Key,
                        shareValue,
                        dateFrom = productionTypeGroup.Min(a => a.measurement.DateFrom),
                        dateTo = productionTypeGroup.Max(a => a.measurement.DateTo),
                        percentageOfTotal = Math.Round(shareValue / totalConsumption / 100, Configuration.DecimalPrecision)
                    };

                //Get period in unix timestamps for entire aggregation.
                var dateFrom = consumptionShares.Min(a => a.dateFrom);
                var dateTo = consumptionShares.Max(a => a.dateTo);

                //Create production type percentage dictionary and calculate renewable percentage for period.
                var sourcesWithPercentage = consumptionShares.ToDictionary(a => a.productionType, b => b.percentageOfTotal);
                var renewablePercentage = sourcesWithPercentage
                    .Where(a => renewableSources.Contains(a.Key))
                    .Sum(a => a.Value * (a.Key == waste ? wasteRenewableShare : 1));

                //Add to results
                result.EnergySources.Add(new EnergySourceDeclaration(dateFrom, dateTo, renewablePercentage, sourcesWithPercentage));
            }

            return result;
        }

        //Translates a date into an aggregated date string.
        //This creates a period bucket/bin spanning the aggregation amount.
        string GetAggregationDateString(DateTime date, Aggregation aggregation)
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
    }
}