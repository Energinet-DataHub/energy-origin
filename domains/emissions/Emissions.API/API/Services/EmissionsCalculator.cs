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

        foreach (var measurement in measurements)
        {
            foreach (var reading in measurement.Measurements)
            {
                var emission = emissions.FirstOrDefault(a =>
                    a.GridArea == measurement.MeteringPoint.GridArea && a.HourUTC == reading.DateFrom.ToDateTime());

                if (emission == null)
                {
                    throw new Exception("Emissions is null");
                }
                var co2 = emission.CO2PerkWh * reading.Quantity;
                listOfEmissions.Add(new Emission
                {
                    Co2 = co2,
                    DateFrom = reading.DateFrom.ToDateTime(),
                    DateTo = reading.DateTo.ToDateTime(),
                    Consumption = reading.Quantity
                });
            }
        }

        IEnumerable<IGrouping<string, Emission>> groupedEmissions = GetGroupedEmissions(aggregation, listOfEmissions);

        var bucketEmissions = new List<Emissions>();
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

        var test = new EmissionsResponse(bucketEmissions);

        return test;
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
                groupedEmissions =
                    listOfEmissions.GroupBy(_ =>
                        _.DateFrom.ToString("yyyy/MM/dd/HH/mm"));
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