using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using API.Services;
using EnergyOriginDateTimeExtension;
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
        // Arrange
        var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2021, 1, 2, 2, 0, 0, DateTimeKind.Utc);
        var timeSeries = CalculateEmissionDataSetFactory.CreateTimeSeries();
        var emissions = CalculateEmissionDataSetFactory.CreateEmissions();

        var calculator = new EmissionsCalculator();

        // Act
        var result = calculator.CalculateEmission(emissions, timeSeries, aggregation).Emissions.ToArray();

        // Assert
        Assert.NotNull(result);
        var expected = GetExpectedEmissions(aggregation, dateFrom, dateTo).ToArray();
        Assert.Equal(expected.Select(_ => (_.Total.Unit, _.Total.Value)), result.Select(_ => (_.Total.Unit, _.Total.Value)));
        Assert.Equal(expected.Select(_ => (_.Relative.Unit, _.Relative.Value)), result.Select(_ => (_.Relative.Unit, _.Relative.Value)));
        Assert.Equal(expected.Select(_ => _.DateFrom), result.Select(_ => _.DateFrom));
        Assert.Equal(expected.Select(_ => _.DateTo), result.Select(_ => _.DateTo));
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
        // Arrange
        var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2021, 1, 2, 2, 0, 0, DateTimeKind.Utc);
        var timeSeries = CalculateEmissionDataSetFactory.CreateTimeSeriesForMismatchMeasurements();
        var emissions = CalculateEmissionDataSetFactory.CreateEmissions();

        var calculator = new EmissionsCalculator();

        // Act
        var result = calculator.CalculateEmission(emissions, timeSeries, aggregation).Emissions.ToArray();

        // Assert
        Assert.NotNull(result);
        var expected = GetExpectedEmissions(aggregation, dateFrom, dateTo).ToArray();
        Assert.Equal(expected.Select(_ => (_.Total.Unit, _.Total.Value)), result.Select(_ => (_.Total.Unit, _.Total.Value)));
        Assert.Equal(expected.Select(_ => (_.Relative.Unit, _.Relative.Value)), result.Select(_ => (_.Relative.Unit, _.Relative.Value)));
        Assert.Equal(expected.Select(_ => _.DateFrom), result.Select(_ => _.DateFrom));
        Assert.Equal(expected.Select(_ => _.DateTo), result.Select(_ => _.DateTo));
    }

    [Theory]
    [InlineData(Aggregation.Total)]
    [InlineData(Aggregation.Actual)]
    [InlineData(Aggregation.Hour)]
    [InlineData(Aggregation.Day)]
    [InlineData(Aggregation.Month)]
    [InlineData(Aggregation.Year)]
    public void CalculateEmission_GivenNoMeasurements_ReturnsEmptyList(Aggregation aggregation)
    {
        // Arrange
        var timeSeries = SourceEmissionShareDataSetFactory.CreateEmptyTimeSeries;
        var emissions = CalculateEmissionDataSetFactory.CreateEmissions();
        var calculator = new EmissionsCalculator();

        // Act
        var result = calculator.CalculateEmission(emissions, timeSeries, aggregation);

        //Assert
        Assert.NotNull(result);
        Assert.Empty(result.Emissions);
    }

    [Fact]
    public void EmissionsAndMeasurements_CalculateTotalEmission_LargeValues()
    {
        // Arrange
        var timeSeries = CalculateEmissionDataSetFactory.CreateTimeSeriesLargeValues();
        var emissions = CalculateEmissionDataSetFactory.CreateEmissions();

        var calculator = new EmissionsCalculator();

        // Act
        var result = calculator.CalculateEmission(emissions, timeSeries, Aggregation.Total).Emissions.ToArray();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(864000000L, result.Single().Total.Value);
    }

    private static IEnumerable<Emissions> GetExpectedEmissions(Aggregation aggregation, DateTime dateFrom, DateTime dateTo) => aggregation switch
    {
        Aggregation.Total => new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateTo.ToUnixTime(),
                        new Quantity(1038.178m, QuantityUnit.g),
                        new Quantity(138.64557m, QuantityUnit.gPerkWh)
                        )
                },
        Aggregation.Actual or Aggregation.Hour => new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddHours(1).ToUnixTime(),
                        new Quantity(250.852m, QuantityUnit.g),
                        new Quantity(124m, QuantityUnit.gPerkWh)
                    ),
                    new(
                        dateFrom.AddHours(1).ToUnixTime(),
                        dateFrom.AddHours(2).ToUnixTime(),
                        new Quantity(340.236m, QuantityUnit.g),
                        new Quantity(234m, QuantityUnit.gPerkWh)
                    ),
                    new(
                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(3).ToUnixTime(),
                        new Quantity(83.13m, QuantityUnit.g),
                        new Quantity(85m, QuantityUnit.gPerkWh)
                    ),
                    new(
                        dateFrom.AddHours(3).ToUnixTime(),
                        dateFrom.AddHours(4).ToUnixTime(),
                        new Quantity(363.96m, QuantityUnit.g),
                        new Quantity(120m, QuantityUnit.gPerkWh)
                    ),
                },
        Aggregation.Day => new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddHours(2).ToUnixTime(),
                        new Quantity(591.088m, QuantityUnit.g),
                        new Quantity(169.99942m, QuantityUnit.gPerkWh)
                    ),
                    new(

                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(4).ToUnixTime(),
                        new Quantity(447.09m, QuantityUnit.g),
                        new Quantity(111.46597m, QuantityUnit.gPerkWh)
                    )
                },
        Aggregation.Month or Aggregation.Year => new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateTo.ToUnixTime(),
                        new Quantity(1038.178m, QuantityUnit.g),
                        new Quantity(138.64557m, QuantityUnit.gPerkWh)
                    )
                },
        _ => throw new NotImplementedException()
    };
}
