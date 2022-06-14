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
public sealed class CalculateConsumptionsTest
{
    readonly CalculateConsumptionsDataSetFactory dataSetFactory = new();

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
        var emissions = dataSetFactory.CreateMeasurements();

        var calculator = new ConsumptionsCalculator();

        // Act
        var result = calculator.CalculateConsumptions(timeSeries, dateFrom.ToUnixTime(),
            dateTo.ToUnixTime(), aggregation).Consumptions.ToArray();

        // Assert
        Assert.NotNull(result);
        var expected = GetExpectedEmissions(aggregation, dateFrom, dateTo).ToArray();
        Assert.Equal(expected.Select(_ => _.Value), result.Select(_ => _.Value));
        Assert.Equal(expected.Select(_ => _.DateFrom), result.Select(_ => _.DateFrom));
        Assert.Equal(expected.Select(_ => _.DateTo), result.Select(_ => _.DateTo));
    }

    IEnumerable<Consumptions> GetExpectedEmissions(Aggregation aggregation, DateTime dateFrom, DateTime dateTo)
    {
        switch (aggregation)
        {
            case Aggregation.Actual:
            case Aggregation.Hour:
                return new List<Consumptions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        1234f
                    ),
                    new(
                        dateFrom.AddHours(1).ToUnixTime(),
                        dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        242f
                    ),
                    new(
                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        654f
                    ),
                    new(
                        dateFrom.AddHours(3).ToUnixTime(),
                        dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        1800f
                    ),
                };
            case Aggregation.Day:
                return new List<Consumptions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        1476f
                    ),
                    new(

                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        2454f
                    )
                };
            case Aggregation.Month:
            case Aggregation.Year:
            case Aggregation.Total:
                return new List<Consumptions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateTo.ToUnixTime(),
                        3930f
                    )
                };
            default:
                return new List<Consumptions>();
        }
    }
}
