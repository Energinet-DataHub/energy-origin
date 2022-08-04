using API.Helpers;
using API.Models;
using EnergyOriginDateTimeExtension;


namespace API.Services;

class EmissionsCalculator : IEmissionsCalculator
{
    public EmissionsResponse CalculateEmission(
        IEnumerable<EmissionRecord> emissions,
        IEnumerable<TimeSeries> measurements,
        DateTime dateFrom,
        DateTime dateTo,
        Aggregation aggregation)
    {

        var listOfEmissions = new List<Emission>();
        var Co2List = new List<decimal>();
        var bucketEmissions = new List<Emissions>();

        foreach (var measurement in measurements)
        {

            var emissionGridArea = emissions.Where(a => a.GridArea.Contains(measurement.MeteringPoint.GridArea)).ToList();

            var measurementTimeMatches = from first in measurement.Measurements.Select(x => x.DateFrom)
                                         join second in emissionGridArea.Select(x => x.HourUTC.ToUnixTime())
                                             on first equals second
                                             select measurement.Measurements.Where(x => x.DateFrom.Equals(first)).Single();

            var emissionsTimeMatches = from first in measurement.Measurements.Select(x => x.DateFrom)
                                       join second in emissionGridArea.Select(x => x.HourUTC.ToUnixTime())
                                             on first equals second
                                             select emissionGridArea.Where(x => x.HourUTC.ToUnixTime().Equals(first)).Single();

            var Quantity = measurementTimeMatches.Select(y => y.Quantity).ToList();
            var CO2PerkWh = emissionsTimeMatches.Select(y => y.CO2PerkWh).ToList();

            Co2List = Quantity.Select((dValue, index) => dValue * CO2PerkWh[index]).ToList();

            foreach (var measure in measurementTimeMatches)
            {
                listOfEmissions.Add(new Emission
                {
                    Co2 = Co2List[0],
                    DateFrom = measure.DateFrom.ToDateTime(),
                    DateTo = measure.DateTo.ToDateTime(),
                    Consumption = measure.Quantity
                });
                Co2List.RemoveAt(0);
            }
        }

        IEnumerable<IGrouping<string, Emission>> groupedEmissions = GetGroupedEmissions(aggregation, listOfEmissions);

        foreach (var groupedEmission in groupedEmissions)
        {
            var totalForBucket = groupedEmission.Sum(_ => _.Co2);
            var relativeForBucket = totalForBucket / groupedEmission.Sum(_ => _.Consumption);
            bucketEmissions.Add(new Emissions(
                groupedEmission.First().DateFrom.ToUnixTime(),
                groupedEmission.Last().DateTo.ToUnixTime(),
                new Quantity(Math.Round(totalForBucket / 1000, Configuration.DecimalPrecision), QuantityUnit.g),
                new Quantity(Math.Round(relativeForBucket, Configuration.DecimalPrecision), QuantityUnit.gPerkWh)
            ));
        }

        var response = new EmissionsResponse(bucketEmissions);

        return response;
    }

    static IEnumerable<IGrouping<string, Emission>> GetGroupedEmissions(Aggregation aggregation, List<Emission> listOfEmissions)
    {
        IEnumerable<IGrouping<string, Emission>> groupedEmissions;
        switch (aggregation)
        {
            case Aggregation.Year:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.DateFrom.Year.ToString());
                break;

            case Aggregation.Month:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM"));
                break;

            case Aggregation.Day:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd"));
                break;

            case Aggregation.Hour:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH"));
                break;

            case Aggregation.QuarterHour:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH/mm"));
                break;

            case Aggregation.Actual:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.DateFrom.ToString("yyyy/MM/dd/HH"));
                break;

            case Aggregation.Total:
                groupedEmissions = listOfEmissions.GroupBy(_ => "total");
                break;

            default:
                throw new ArgumentOutOfRangeException($"Invalid value {nameof(aggregation)}");
        }

        return groupedEmissions;
    }
}

internal class Emission
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal Co2 { get; set; }
    public int Consumption { get; set; }
}
