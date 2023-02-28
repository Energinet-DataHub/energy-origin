using API.Helpers;
using API.Models;
using API.Models.EnergiDataService;

namespace API.Services;

internal class EmissionsCalculator : IEmissionsCalculator
{
    public EmissionsResponse CalculateEmission(
        IEnumerable<EmissionRecord> emissions,
        IEnumerable<TimeSeries> timeSeriesList,
        TimeZoneInfo timeZone,
        Aggregation aggregation)
    {
        var bucketEmissions = new List<Emissions>();

        var listOfEmissions = timeSeriesList.SelectMany(timeseries => timeseries.Measurements.Join(
                emissions,
                measurement => new Tuple<string, long>(timeseries.MeteringPoint.GridArea, measurement.DateFrom),
                record => new Tuple<string, long>(record.GridArea, new DateTimeOffset(record.HourUTC).ToUnixTimeSeconds()),
                (measurement, record) => new Emission
                (
                    Co2: measurement.Quantity * record.CO2PerkWh,
                    DateFrom: DateTimeOffset.FromUnixTimeSeconds(measurement.DateFrom),
                    DateTo: DateTimeOffset.FromUnixTimeSeconds(measurement.DateTo),
                    Consumption: measurement.Quantity
                )));

        var groupedEmissions = GetGroupedEmissions(aggregation, timeZone, listOfEmissions);

        foreach (var groupedEmission in groupedEmissions)
        {
            var totalForBucket = groupedEmission.Sum(x => x.Co2);
            var relativeForBucket = totalForBucket / groupedEmission.Sum(x => x.Consumption);
            bucketEmissions.Add(new Emissions(
                groupedEmission.First().DateFrom.ToUnixTimeSeconds(),
                groupedEmission.Last().DateTo.ToUnixTimeSeconds(),
                new Quantity(Math.Round(totalForBucket / 1000, Configuration.DecimalPrecision), QuantityUnit.g),
                new Quantity(Math.Round(relativeForBucket, Configuration.DecimalPrecision), QuantityUnit.gPerkWh)
            ));
        }

        var response = new EmissionsResponse(bucketEmissions);

        return response;
    }

    private static IEnumerable<IGrouping<string, Emission>> GetGroupedEmissions(Aggregation aggregation, TimeZoneInfo timeZone, IEnumerable<Emission> listOfEmissions)
    {
        var groupedEmissions = aggregation switch
        {
            Aggregation.Year => listOfEmissions.GroupBy(x => x.DateFrom.Year.ToString()),
            Aggregation.Month => listOfEmissions.GroupBy(x => x.DateFrom.ToString("yyyy/MM")),
            Aggregation.Day => listOfEmissions.GroupBy(x => x.DateFrom.ToString("yyyy/MM/dd")),
            Aggregation.Hour => listOfEmissions.GroupBy(x => x.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.QuarterHour => listOfEmissions.GroupBy(x => x.DateFrom.ToString("yyyy/MM/dd/HH/mm")),
            Aggregation.Actual => listOfEmissions.GroupBy(x => x.DateFrom.ToString("yyyy/MM/dd/HH")),
            Aggregation.Total => listOfEmissions.GroupBy(_ => "total"),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation)),
        };
        return groupedEmissions;
    }

    private record Emission
    (
        DateTimeOffset DateFrom,
        DateTimeOffset DateTo,
        decimal Co2,
        long Consumption
    );
}
