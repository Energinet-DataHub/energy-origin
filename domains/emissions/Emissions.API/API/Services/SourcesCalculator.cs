using API.Helpers;
using API.Models;

namespace API.Services
{
    public class SourcesCalculator : ISourcesCalculator
    {
        public EnergySourceResponse CalculateSourceEmissions(IEnumerable<TimeSeries> measurements, DeclarationProduction declaration,
            long dateFrom, long dateTo, Aggregation aggregation)
        {
            EnergySourceResponse result;

            IEnumerable<IGrouping<string, Record>> groupedDeclarations = GetGroupedDeclarations(aggregation, declaration.Result.Records);

            var consumptionResult = new Dictionary<string, List<ConsumptionShare>>();
            foreach (var timeSeries in measurements)
            {
                foreach (var measurement in timeSeries.Measurements)
                {
                    var utcDateTime = GetDateAsString(measurement.DateFrom.ToUtcDateTime(), aggregation);
                    var gridArea = timeSeries.MeteringPoint.GridArea;
                    var totalShares = groupedDeclarations.First(_ => 
                        _.Key == utcDateTime + gridArea
                    );
                    consumptionResult.TryGetValue(utcDateTime + gridArea, out var shares);
                    if (shares == null)
                    {
                        shares = new List<ConsumptionShare>();
                        consumptionResult.Add(utcDateTime + gridArea, shares);
                    }

                    foreach (var totalShare in totalShares)
                    {
                        shares.Add(new ConsumptionShare(totalShare.ShareTotal * measurement.Quantity), );

                    }
                }  
            }
            return result;
        }

        private IEnumerable<IGrouping<string, Record>> GetGroupedDeclarations(Aggregation aggregation, List<Record> declaration)
        {
            if (aggregation == Aggregation.Total)
                return declaration.GroupBy(_ => "total");
            
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(aggregation), aggregation, null);
            }

            
        }

        class ConsumptionShare
        {
            public ConsumptionShare(float value, float measurementTime, string productionType)
            {
                Value = value;
                Date = measurementTime;
                ProductionType = productionType;
            }

            public float Value { get; }

            public long Date { get; }
            
            public string ProductionType { get; }
        }
    }
}
