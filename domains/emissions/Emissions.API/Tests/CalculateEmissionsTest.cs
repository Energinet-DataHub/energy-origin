using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using API.Services;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class CalculateEmissionsTest
{
    [Theory]
    [InlineData(Aggregation.Total)]
    [InlineData(Aggregation.Actual)]
    [InlineData(Aggregation.Hour)]
    [InlineData(Aggregation.Day)]
    [InlineData(Aggregation.Month)]
    [InlineData(Aggregation.Year)]
    public void EmissionsAndMeasurements_CalculateTotalEmission_TotalAnRelativeEmission(Aggregation aggregation)
    {
        var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2021, 1, 2, 2, 0, 0, DateTimeKind.Utc);
        var timeSeries = CalculateEmissionDataSetFactory.CreateTimeSeries();
        var emissions = CalculateEmissionDataSetFactory.CreateEmissions();
        var calculator = new EmissionsCalculator();

        var result = calculator.CalculateEmission(emissions, timeSeries, TimeZoneInfo.Utc, aggregation).Emissions.ToArray();

        Assert.NotNull(result);
        var expected = GetExpectedEmissions(aggregation, dateFrom, dateTo).ToArray();
        Assert.Equal(expected.Select(x => (x.Total.Unit, x.Total.Value)), result.Select(x => (x.Total.Unit, x.Total.Value)));
        Assert.Equal(expected.Select(x => (x.Relative.Unit, x.Relative.Value)), result.Select(x => (x.Relative.Unit, x.Relative.Value)));
        Assert.Equal(expected.Select(x => x.DateFrom), result.Select(x => x.DateFrom));
        Assert.Equal(expected.Select(x => x.DateTo), result.Select(x => x.DateTo));
    }

    [Theory]
    [InlineData(Aggregation.Total)]
    [InlineData(Aggregation.Actual)]
    [InlineData(Aggregation.Hour)]
    [InlineData(Aggregation.Day)]
    [InlineData(Aggregation.Month)]
    [InlineData(Aggregation.Year)]
    public void EmissionsAndMeasurements_CalculateTotalEmission_MismatchBetweenDatasetSize(Aggregation aggregation)
    {
        var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2021, 1, 2, 2, 0, 0, DateTimeKind.Utc);
        var timeSeries = CalculateEmissionDataSetFactory.CreateTimeSeriesForMismatchMeasurements();
        var emissions = CalculateEmissionDataSetFactory.CreateEmissions();
        var calculator = new EmissionsCalculator();

        var result = calculator.CalculateEmission(emissions, timeSeries, TimeZoneInfo.Utc, aggregation).Emissions.ToArray();

        Assert.NotNull(result);
        var expected = GetExpectedEmissions(aggregation, dateFrom, dateTo).ToArray();
        Assert.Equal(expected.Select(_ => (_.Total.Unit, _.Total.Value)), result.Select(_ => (_.Total.Unit, _.Total.Value)));
        Assert.Equal(expected.Select(_ => (_.Relative.Unit, _.Relative.Value)), result.Select(_ => (_.Relative.Unit, _.Relative.Value)));
        Assert.Equal(expected.Select(x => x.DateFrom), result.Select(x => x.DateFrom));
        Assert.Equal(expected.Select(x => x.DateTo), result.Select(x => x.DateTo));
    }

    [Fact]
    public void EmissionsAndMeasurements_CalculateTotalEmission_HugeDateSet()
    {
        var timeSeries = CalculateEmissionDataSetFactory.CreateTimeSeriesHugeValues();
        var emissions = CalculateEmissionDataSetFactory.CreateEmissions();
        var calculator = new EmissionsCalculator();

        var result = calculator.CalculateEmission(emissions, timeSeries, TimeZoneInfo.Utc, Aggregation.Total).Emissions.ToArray();

        Assert.NotNull(result);
        Assert.Equal(864000000L, result.Single().Total.Value);
    }

    private static IEnumerable<Emissions> GetExpectedEmissions(Aggregation aggregation, DateTimeOffset dateFrom, DateTimeOffset dateTo) => aggregation switch
    {
        Aggregation.Total => new List<Emissions>()
        {
            new(
                dateFrom.ToUnixTimeSeconds(),
                dateTo.ToUnixTimeSeconds(),
                new Quantity(1038.178m, QuantityUnit.g),
                new Quantity(138.64557m, QuantityUnit.gPerkWh)
            )
        },
        Aggregation.Actual or Aggregation.Hour => new List<Emissions>()
        {
            new(
                dateFrom.ToUnixTimeSeconds(),
                dateFrom.AddHours(1).ToUnixTimeSeconds(),
                new Quantity(250.852m, QuantityUnit.g),
                new Quantity(124m, QuantityUnit.gPerkWh)
            ),
            new(
                dateFrom.AddHours(1).ToUnixTimeSeconds(),
                dateFrom.AddHours(2).ToUnixTimeSeconds(),
                new Quantity(340.236m, QuantityUnit.g),
                new Quantity(234m, QuantityUnit.gPerkWh)
            ),
            new(
                dateFrom.AddHours(2).ToUnixTimeSeconds(),
                dateFrom.AddHours(3).ToUnixTimeSeconds(),
                new Quantity(83.13m, QuantityUnit.g),
                new Quantity(85m, QuantityUnit.gPerkWh)
            ),
            new(
                dateFrom.AddHours(3).ToUnixTimeSeconds(),
                dateFrom.AddHours(4).ToUnixTimeSeconds(),
                new Quantity(363.96m, QuantityUnit.g),
                new Quantity(120m, QuantityUnit.gPerkWh)
            ),
        },
        Aggregation.Day => new List<Emissions>()
        {
            new(
                dateFrom.ToUnixTimeSeconds(),
                dateFrom.AddHours(2).ToUnixTimeSeconds(),
                new Quantity(591.088m, QuantityUnit.g),
                new Quantity(169.99942m, QuantityUnit.gPerkWh)
            ),
            new(
                dateFrom.AddHours(2).ToUnixTimeSeconds(),
                dateFrom.AddHours(4).ToUnixTimeSeconds(),
                new Quantity(447.09m, QuantityUnit.g),
                new Quantity(111.46597m, QuantityUnit.gPerkWh)
            )
        },
        Aggregation.Month or Aggregation.Year => new List<Emissions>()
        {
            new(
                dateFrom.ToUnixTimeSeconds(),
                dateTo.ToUnixTimeSeconds(),
                new Quantity(1038.178m, QuantityUnit.g),
                new Quantity(138.64557m, QuantityUnit.gPerkWh)
            )
        },
        _ => new List<Emissions>(),
    };
}
