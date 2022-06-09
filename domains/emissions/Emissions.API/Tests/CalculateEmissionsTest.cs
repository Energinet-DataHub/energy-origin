using System;
using System.Collections.Generic;
using System.Linq;
using API.Helpers;
using API.Models;
using API.Services;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class CalculateEmissionsTest
{
    readonly CalculateEmissionDataSetFactory dataSetFactory = new();

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
        var dateTo = new DateTime(2021, 1, 2, 1, 59, 59, DateTimeKind.Utc);
        var timeSeries = dataSetFactory.CreateTimeSeries();
        var emissions = dataSetFactory.CreateEmissions();

        var calculator = new EmissionsCalculator();

        // Act
        var result = calculator.CalculateEmission(emissions, timeSeries, dateFrom.ToUnixTime(),
            dateTo.ToUnixTime(), aggregation).Emissions.ToArray();

        // Assert
        Assert.NotNull(result);
        var expected = GetExpectedEmissions(aggregation, dateFrom, dateTo).ToArray();
        Assert.Equal(expected.Select(_ => _.Total), result.Select(_ => _.Total));
        Assert.Equal(expected.Select(_ => _.Relative), result.Select(_ => _.Relative));
        Assert.Equal(expected.Select(_ => _.DateFrom), result.Select(_ => _.DateFrom));
        Assert.Equal(expected.Select(_ => _.DateTo), result.Select(_ => _.DateTo));
    }

    IEnumerable<Emissions> GetExpectedEmissions(Aggregation aggregation, DateTime dateFrom, DateTime dateTo)
    {
        switch (aggregation)
        {
            case Aggregation.Total:
                return new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateTo.ToUnixTime(),
                        new Quantity(481.234f, QuantityUnit.g),
                        new Quantity(122.4514f, QuantityUnit.gPerkWh)
                        )

                };
            case Aggregation.Actual:
            case Aggregation.Hour:
                return new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Quantity(153.016f, QuantityUnit.g),
                        new Quantity(124f, QuantityUnit.gPerkWh)
                    ),
                    new(
                        dateFrom.AddHours(1).ToUnixTime(),
                        dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Quantity(56.628f, QuantityUnit.g),
                        new Quantity(234f, QuantityUnit.gPerkWh)
                    ),
                    new(
                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Quantity(55.59f, QuantityUnit.g),
                        new Quantity(85f, QuantityUnit.gPerkWh)
                    ),
                    new(
                        dateFrom.AddHours(3).ToUnixTime(),
                        dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Quantity(216f, QuantityUnit.g),
                        new Quantity(120f, QuantityUnit.gPerkWh)
                    ),
                };
            case Aggregation.Day:
                return new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Quantity(209.644f, QuantityUnit.g),
                        new Quantity(142.03523f, QuantityUnit.gPerkWh)
                    ),
                    new(

                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Quantity(271.59f, QuantityUnit.g),
                        new Quantity(110.67237f, QuantityUnit.gPerkWh)
                    )
                };
            case Aggregation.Month:
            case Aggregation.Year:
                return new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateTo.ToUnixTime(),
                        new Quantity(481.234f, QuantityUnit.g),
                        new Quantity(122.4514f, QuantityUnit.gPerkWh)
                    )
                };
            default:
                return new List<Emissions>();
        }
    }
}
