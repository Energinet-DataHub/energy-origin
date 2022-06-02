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
    readonly DataSetFactory dataSetFactory = new();

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


        var sut = new EmissionsCalculator();

        // Act
        var result = sut.CalculateEmission(emissions, timeSeries, dateFrom.ToUnixTime(),
            dateTo.ToUnixTime(), aggregation).Emissions.ToArray();

        // Assert
        Assert.NotNull(result);
        var emissionsEnumerable = GetExpectedEmissions(aggregation, dateFrom, dateTo).ToArray();
        Assert.Equal(emissionsEnumerable.Select(_ => _.Total.Co2), result.Select(_ => _.Total.Co2));
        Assert.Equal(emissionsEnumerable.Select(_ => _.Relative.Co2), result.Select(_ => _.Relative.Co2));
        Assert.Equal(emissionsEnumerable.Select(_ => _.DateFrom), result.Select(_ => _.DateFrom));
        Assert.Equal(emissionsEnumerable.Select(_ => _.DateTo), result.Select(_ => _.DateTo));
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
                        new Total( 481.234f ),
                        new Relative(122.4514f)
                        )

                };
            case Aggregation.Actual:
            case Aggregation.Hour:
                return new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Total(153.016f),
                        new Relative(124f)
                    ),
                    new(
                        dateFrom.AddHours(1).ToUnixTime(),
                        dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Total(56.628f),
                        new Relative(234f)
                    ),
                    new(
                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(2).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Total(55.59f),
                        new Relative(85f)
                    ),
                    new(
                        dateFrom.AddHours(3).ToUnixTime(),
                        dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Total(216f),
                        new Relative(120f)
                    ),
                };
            case Aggregation.Day:
                return new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateFrom.AddHours(1).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Total(209.644f),
                        new Relative(142.03523f)
                    ),
                    new(

                        dateFrom.AddHours(2).ToUnixTime(),
                        dateFrom.AddHours(3).AddMinutes(59).AddSeconds(59).ToUnixTime(),
                        new Total(271.59f),
                        new Relative(110.67237f)
                    )
                };
            case Aggregation.Month:
            case Aggregation.Year:
                return new List<Emissions>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateTo.ToUnixTime(),
                        new Total(481.234f),
                        new Relative(122.4514f)
                    )
                };
            default:
                return new List<Emissions>();
        }
    }
}