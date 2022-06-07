using API.Helpers;
using API.Models;


namespace API.Services
{
    public class SourcesCalculator : ISourcesCalculator
    {
        public EnergySourceResponse CalculateSourceEmissions(IEnumerable<TimeSeries> timeSeries, DeclarationProduction declaration,
            long dateFrom, long dateTo, Aggregation aggregation)
        {
            EnergySourceResponse result = new EnergySourceResponse(new List<EnergySourceDeclaration>());

            IEnumerable<IGrouping<string, Record>> groupedDeclarations = GetGroupedDeclarations(aggregation, declaration.Result.Records);
            var consumptionResults = new Dictionary<string, List<ConsumptionShare>>();

            foreach (var measurements in timeSeries)
            {
                foreach (var measurement in measurements.Measurements)
                {
                    var utcDateTime = GetDateAsString(measurement.DateFrom.ToUtcDateTime(), aggregation);
                    var gridArea = measurements.MeteringPoint.GridArea;
                    var key = utcDateTime + gridArea;
                    var totalShares = groupedDeclarations.Single(_ =>
                        _.Key == key
                    );
                    consumptionResults.TryGetValue(utcDateTime + gridArea, out var shares);
                    if (shares == null)
                    {
                        shares = new List<ConsumptionShare>();
                        consumptionResults.Add(utcDateTime + gridArea, shares);
                    }


                    var query =
                        from totalShare in totalShares
                        group shares by new { totalShare.HourUTC, totalShare.PriceArea, totalShare.ProductionType } into groupedShares
                        select new ConsumptionShare
                        (
                            groupedShares.Select(a => a.Sum(s => s.Value * measurement.Quantity)).Sum(),
                            groupedShares.Key.HourUTC.ToUnixTime(),
                            groupedShares.Key.ProductionType
                        );
                    shares.AddRange(query);

                    var totalQuantity =+ measurement.Quantity;
                }
            }
            foreach (var consumptionResult in consumptionResults)
            {
                var productionTypeValues =
                    from consumption in consumptionResult.Value
                    group consumption by consumption.ProductionType into groupValues
                    select new EnergySourceDeclaration
                    (
                        groupValues.Min(_ => _.Date),
                        groupValues.Max(_ => _.Date),
                        CalculateRenewable(groupValues),
                        groupValues.ToDictionary(x => x.ProductionType, x => x.Value)
                    );
            }

            IEnumerable<IGrouping<string, Measurement>> groupedMeasurements = GetGroupedMeasurements(aggregation, timeSeries);


            EnergySourceResponse EnergySources = null;
            result = EnergySources;
            return result;
        }

        private float CalculateRenewable(IGrouping<string, ConsumptionShare> groupValues)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<IGrouping<string, Measurement>> GetGroupedMeasurements(Aggregation aggregation, IEnumerable<TimeSeries> timeSeries)
        {
            if (aggregation == Aggregation.Total)
            {
                return timeSeries.SelectMany(_ => _.Measurements).GroupBy(x => "total");
            }

            return timeSeries.SelectMany(y => y.Measurements)
                .GroupBy(x => GetDateAsString(x.DateFrom.ToUtcDateTime(), aggregation));
        }

        private IEnumerable<IGrouping<string, Record>> GetGroupedDeclarations(Aggregation aggregation, List<Record> declaration)
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
            public ConsumptionShare(float value, long date, string productionType)
            {
                Value = value;
                Date = date;
                ProductionType = productionType;
            }

            public float Value { get; }

            public long Date { get; }

            public string ProductionType { get; }

        }
    }
}
