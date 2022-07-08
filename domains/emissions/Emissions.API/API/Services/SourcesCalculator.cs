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
        const int decimalPrecision = 5;

        public EnergySourceResponse CalculateSourceEmissions(
            IEnumerable<TimeSeries> timeSeries,
            IEnumerable<MixRecord> records,
            Aggregation aggregation)
        {
            //TODO: Check if it makes sense to treat 'aggregation total' separately.

            //Get declarations lookup using aggregation and grid area as key.
            //When expressing a date as a string using an aggregation, it becomes a period.
            var declarationLookup = GetDeclarationLookup(aggregation, records);
                        
            //Dictionary of consumption results. Outer key is the measurement
            //date aggretated string (period), inner key is the declaration production type.
            var consumptionResults = new Dictionary<string, Dictionary<string, ConsumptionShare>>();

            //Go through all measurements in all timeseries.
            foreach (var singleTimeSeries in timeSeries)
                foreach (var measurement in singleTimeSeries.Measurements)
                {
                    //Get measurement date as string, based on aggregation (period).
                    var aggregatedDateTime = GetAggregationDateString(measurement.DateFrom, aggregation);

                    //Look up declarations by measurement period and metering point grid area.
                    var gridArea = singleTimeSeries.MeteringPoint.GridArea;
                    var declarations = declarationLookup[aggregatedDateTime + gridArea]
                        .Where(a => a.HourUTC == measurement.DateFrom);
                                        
                    CalculateConsumptionShareByReference(consumptionResults, aggregatedDateTime, declarations, measurement);
                }

            var result = CalculateSourceEmissionPercentage(timeSeries, aggregation, consumptionResults);

            return result;
        }

        void CalculateConsumptionShareByReference(
            Dictionary<string, Dictionary<string, ConsumptionShare>> consumptionResults,
            string aggregatedDateTime,
            IEnumerable<MixRecord> declarations,
            Measurement measurement)
        {
            //Check if dictionary contains the aggregated time key.
            if (!consumptionResults.TryGetValue(aggregatedDateTime, out var shares))
            {
                //If not, add new dictionary for the aggregated time value. 
                shares = new Dictionary<string, ConsumptionShare>();
                consumptionResults.Add(aggregatedDateTime, shares);
            }

            //Go through all declarations.
            foreach (var declaration in declarations)
            {
                //Check if dictionary contains a consumtionshare with
                //the current declaration's production type.
                if (!shares.TryGetValue(declaration.ProductionType, out var share))
                {
                    //If not, add a new consumption share for the production type.
                    share = new ConsumptionShare
                    (
                        0,
                        measurement.DateFrom,
                        measurement.DateTo,
                        declaration.ProductionType
                    );
                    shares.Add(declaration.ProductionType, share);
                }

                //Multiply the declaration share total with the measurement
                //quantity and add it to the consumtion share value.
                share.Value += declaration.ShareTotal * measurement.Quantity;

                //Update to the earliest from date.
                if (measurement.DateFrom < share.DateFrom)
                    share.DateFrom = measurement.DateFrom;

                //Update to the latest to date.
                if (measurement.DateTo > share.DateTo)
                    share.DateTo = measurement.DateTo;
            }
        }

        EnergySourceResponse CalculateSourceEmissionPercentage(
            IEnumerable<TimeSeries> timeSeries,
            Aggregation aggregation,
            Dictionary<string, Dictionary<string, ConsumptionShare>> consumptionResults)
        {
            var result = new EnergySourceResponse(new List<EnergySourceDeclaration>());

            //Get measurement lookup using measurement aggregation date string as key.
            //TODO: Move this out of the CalculateSourceEmissions nested foreach loops!
            var measurementLookup = GetMeasurementLookup(aggregation, timeSeries);

            //Go through the dictionary of consumption results.
            foreach (var consumptionResult in consumptionResults)
            {
                //Get the measurements matching the consumption result
                //key (aggregation date string) and sum their quantities.
                var matchingConsumptionSum = measurementLookup[consumptionResult.Key].Sum(a => a.Quantity);

                //Calculate the percentage of each energy source in relation
                //to the entire consumption within the current period.
                var sourcesWithPercentage = consumptionResult.Value.ToDictionary(a =>
                    a.Key, b => Math.Round(b.Value.Value / matchingConsumptionSum / 100, decimalPrecision));

                //Calculate the percentage of renawable energy sources.
                var renewablePercentage = CalculateRenewable(sourcesWithPercentage);

                //Add a new declaration with a period, source percentages and renewable energy percentage.
                result.EnergySources.Add(new EnergySourceDeclaration
                (
                    consumptionResult.Value.Min(a => a.Value.DateFrom).ToUnixTime(),
                    consumptionResult.Value.Max(a => a.Value.DateTo).ToUnixTime(),
                    renewablePercentage,
                    sourcesWithPercentage
                ));
            }

            return result;
        }

        decimal CalculateRenewable(IDictionary<string, decimal> groupValues) =>
             groupValues.Where(a => renewableSources.Contains(a.Key)).
                    Sum(a => a.Value * (a.Key == waste ? wasteRenewableShare : 1));

        //Creates a lookup using the aggregated measurement date as the key.
        //This groups the measurements for whatever period the aggregation covers.
        //When the aggregation is 'total', there is effectively only a single group.
        ILookup<string, Measurement> GetMeasurementLookup(Aggregation aggregation, IEnumerable<TimeSeries> timeSeries)
        {
            if (aggregation == Aggregation.Total)
                return timeSeries.SelectMany(a => a.Measurements).ToLookup(x => total);

            return timeSeries.SelectMany(y => y.Measurements)
                .ToLookup(x => GetAggregationDateString(x.DateFrom, aggregation));
        }

        //Create a lookup using the aggregated declaration date + grid area as the key.
        //This groups the declarations for each grid area and whatever period the aggregation covers.
        //When the aggregation is 'total', declarations are effectively grouped only on grid area.
        ILookup<string, MixRecord> GetDeclarationLookup(Aggregation aggregation, IEnumerable<MixRecord> declaration)
        {
            if (aggregation == Aggregation.Total)
                return declaration.ToLookup(a => total + a.GridArea);

            return declaration.ToLookup(a => GetAggregationDateString(a.HourUTC, aggregation) + a.GridArea);
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

        //Intermediary DTO used only in calculations.
        class ConsumptionShare
        {
            public decimal Value { get; set; }
            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }
            public string ProductionType { get; }

            public ConsumptionShare(decimal value, DateTime dateFrom, DateTime dateTo, string productionType)
            {
                Value = value;
                DateFrom = dateFrom;
                DateTo = dateTo;
                ProductionType = productionType;
            }
        }
    }
}