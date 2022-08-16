using API.Helpers;
using API.Models;
using EnergyOriginDateTimeExtension;


namespace API.Services;

class EmissionsCalculator : IEmissionsCalculator
{
    public EmissionsResponse CalculateEmission(
        IEnumerable<EmissionRecord> emissions,
        IEnumerable<TimeSeries> timeSeriesList,
        DateTime dateFrom,
        DateTime dateTo,
        Aggregation aggregation)
    {

        var bucketEmissions = new List<Emissions>();

        var listOfEmissions = timeSeriesList.SelectMany(timeseries => timeseries.Measurements.Join(
                emissions,
                measurement => new Tuple<string, long>(timeseries.MeteringPoint.GridArea, measurement.DateFrom),
                record => new Tuple<string, long>(record.GridArea, record.HourUTC.ToUnixTime()),
                (measurement, record) => new Emission
                {
                    Co2 = measurement.Quantity * record.CO2PerkWh,
                    DateFrom = measurement.DateFrom.ToDateTime(),
                    DateTo = measurement.DateTo.ToDateTime(),
                    Consumption = measurement.Quantity
                }));

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

    static IEnumerable<IGrouping<string, Emission>> GetGroupedEmissions(Aggregation aggregation, IEnumerable<Emission> listOfEmissions)
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

internal record Emission
{
    public DateTime DateFrom { get; init; }
    public DateTime DateTo { get; init; }
    public decimal Co2 { get; init; }
    public long Consumption { get; init; }
}
