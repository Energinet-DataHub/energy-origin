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

            var consumptionResult = new Dictionary<string, Dictionary<string, ConsumptionShare>>();
            foreach (var meteringPointTimeSeries in timeSeries)
            {
                foreach (var measurement in meteringPointTimeSeries.Measurements)
                {
                    var utcDateTime = GetDateAsString(measurement.DateFrom.ToUtcDateTime(), aggregation);
                    var gridArea = meteringPointTimeSeries.MeteringPoint.GridArea;
                    var key = utcDateTime + gridArea;
                    var totalShares = groupedDeclarations.Single(_ =>
                        _.Key == key
                    );
                    consumptionResult.TryGetValue(key, out var shares);
                    if (shares == null)
                    {
                        shares = new Dictionary<string,ConsumptionShare>();
                        consumptionResult.Add(key, shares);
                    }

                    foreach (var totalShare in totalShares)
                    {
                        if (!shares.TryGetValue(totalShare.ProductionType, out var share))
                        {
                            share = new ConsumptionShare {
                                Value = 0, 
                                Date = measurement.DateFrom, 
                                ProductionType = totalShare.ProductionType};
                            shares.Add(totalShare.ProductionType, share);
                        }
                        share.Value += (float)totalShare.ShareTotal * measurement.Quantity;
                    }
                }
            }

            IEnumerable<IGrouping<string, Measurement>> groupedMeasurements = GetGroupedMeasurements(aggregation, timeSeries);

            return result;
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
            public float Value { get; set; }

            public long Date { get; set; }

            public string ProductionType { get; set; }

        }
    }
}
