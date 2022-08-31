using API.Helpers;
using API.Models;
using EnergyOriginDateTimeExtension;


namespace API.Services;

public class EmissionsCalculator : IEmissionsCalculator
{
    public EmissionsResponse CalculateEmission(
        IEnumerable<EmissionRecord> emissions,
        IEnumerable<TimeSeries> timeSeriesList,
        Aggregation aggregation)
    {

        var bucketEmissions = new List<Emissions>();

        var listOfEmissions = timeSeriesList.SelectMany(timeseries => timeseries.Measurements.Join(
                emissions,
                measurement => new Tuple<string, long>(timeseries.MeteringPoint.GridArea, measurement.DateFrom),
                record => new Tuple<string, long>(record.GridArea, record.HourUTC.ToUnixTime()),
                (measurement, record) => new Emission
                (
                    Co2: measurement.Quantity * record.CO2PerkWh,
                    DateFrom: measurement.DateFrom.ToDateTime(),
                    DateTo: measurement.DateTo.ToDateTime(),
                    Consumption: measurement.Quantity
                )));

        var groupedEmissions = GetGroupedEmissions(aggregation, listOfEmissions);

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

    private static IEnumerable<IGrouping<string, Emission>> GetGroupedEmissions(Aggregation aggregation, IEnumerable<Emission> emissions) => aggregation switch
    {
        Aggregation.Year => emissions.GroupBy(emission => emission.DateFrom.Year.ToString()),
        Aggregation.Month => emissions.GroupBy(emission => emission.DateFrom.ToString("yyyy/MM")),
        Aggregation.Day => emissions.GroupBy(emission => emission.DateFrom.ToString("yyyy/MM/dd")),
        Aggregation.Hour => emissions.GroupBy(emission => emission.DateFrom.ToString("yyyy/MM/dd/HH")),
        Aggregation.QuarterHour => emissions.GroupBy(emission => emission.DateFrom.ToString("yyyy/MM/dd/HH/mm")),
        Aggregation.Actual => emissions.GroupBy(emission => emission.DateFrom.ToString("yyyy/MM/dd/HH")),
        Aggregation.Total => emissions.GroupBy(emission => "total"),
        _ => throw new ArgumentOutOfRangeException($"Invalid value {aggregation}"),
    };
}

internal record Emission
(
    DateTime DateFrom,
    DateTime DateTo,
    decimal Co2,
    long Consumption
);
