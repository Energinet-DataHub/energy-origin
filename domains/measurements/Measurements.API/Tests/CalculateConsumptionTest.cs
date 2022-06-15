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
public sealed class CalculateConsumptionTest
{
    readonly CalculateConsumptionDataSetFactory dataSetFactory = new();

    [Theory]
    [InlineData(Aggregation.Total)]
    [InlineData(Aggregation.Actual)]
    [InlineData(Aggregation.Hour)]
    [InlineData(Aggregation.Day)]
    [InlineData(Aggregation.Month)]
    [InlineData(Aggregation.Year)]
    public void Measurements_CalculateConsumption_Aggregation(Aggregation aggregation)
    {
        // Arrange
        var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2021, 1, 2, 1, 59, 59, DateTimeKind.Utc);
        var timeSeries = dataSetFactory.CreateTimeSeries();

        var calculator = new ConsumptionCalculator();

        // Act
        var result = calculator.CalculateConsumption(timeSeries, dateFrom.ToUnixTime(),
            dateTo.ToUnixTime(), aggregation).Consumption.ToArray();

        // Assert
        Assert.NotNull(result);
        var expected = GetExpectedConsumption(aggregation, dateFrom, dateTo).ToArray();
        Assert.Equal(expected.Select(_ => _.Value), result.Select(_ => _.Value));
        Assert.Equal(expected.Select(_ => _.DateFrom), result.Select(_ => _.DateFrom));
        Assert.Equal(expected.Select(_ => _.DateTo), result.Select(_ => _.DateTo));
    }

    //TheoryData<long, long, float> expectedHour => new TheoryData<long, long, float>
    //{
    //   { new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(), new DateTime(2021, 1, 1, 22, 59, 59, DateTimeKind.Utc).ToUnixTime(), 1234f },
    //   { new DateTime(2021, 1, 1, 23, 0, 0, DateTimeKind.Utc).ToUnixTime(), new DateTime(2021, 1, 1, 23, 59, 59, DateTimeKind.Utc).ToUnixTime(), 1234f },
    //   { new DateTime(2021, 1, 2, 0,  0, 0, DateTimeKind.Utc).ToUnixTime(), new DateTime(2021, 1, 2, 0,  59, 59, DateTimeKind.Utc).ToUnixTime(), 1234f },
    //   { new DateTime(2021, 1, 2, 1,  0, 0, DateTimeKind.Utc).ToUnixTime(), new DateTime(2021, 1, 2, 1,  59, 59, DateTimeKind.Utc).ToUnixTime(), 1234f },
    //};

    //public static class TestData
    //{
    //    private static Consumption Consumption1 = new Consumption(
    //        new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc).ToUnixTime(),
    //        new DateTime(2021, 1, 1, 22, 59, 59, DateTimeKind.Utc).ToUnixTime(),
    //        1234f);
    //    private static Consumption Consumption2 = new Consumption(
    //        new DateTime(2021, 1, 1, 23, 0, 0, DateTimeKind.Utc).ToUnixTime(),
    //        new DateTime(2021, 1, 1, 23, 59, 59, DateTimeKind.Utc).ToUnixTime(),
    //        1234f);
    //    private static Consumption Consumption3 = new Consumption(
    //        new DateTime(2021, 1, 2, 0, 0, 0, DateTimeKind.Utc).ToUnixTime(),
    //        new DateTime(2021, 1, 2, 0, 59, 59, DateTimeKind.Utc).ToUnixTime(),
    //        1234f);
    //    private static Consumption Consumption4 = new Consumption(
    //        new DateTime(2021, 1, 2, 1, 0, 0, DateTimeKind.Utc).ToUnixTime(),
    //        new DateTime(2021, 1, 2, 1, 59, 59, DateTimeKind.Utc).ToUnixTime(),
    //        1234f);
    //    public static List<Consumption> dummyHourConsumptionList
    //    {
    //        get
    //        {
    //            return new List<Consumption>()
    //            {
    //                Consumption1,
    //                Consumption2,
    //                Consumption3,
    //                Consumption4,
    //            };
    //        }
    //    }
    //}

    IEnumerable<Consumption> GetExpectedConsumption(Aggregation aggregation, DateTime dateFrom, DateTime dateTo)
    {
        return aggregation switch
        {
            Aggregation.Actual or Aggregation.Hour => new List<Consumption>()
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
                },
            Aggregation.Day => new List<Consumption>()
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
                },
            Aggregation.Month or Aggregation.Year or Aggregation.Total => new List<Consumption>()
                {
                    new(
                        dateFrom.ToUnixTime(),
                        dateTo.ToUnixTime(),
                        3930f
                    )
                },
            _ => new List<Consumption>(),
        };
    }
}
