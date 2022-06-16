using System;
using System.Linq;
using API.Helpers;
using API.Models;
using API.Services;
using Xunit;
using Xunit.Categories;
using EnergyOriginDateTimeExtension;

namespace Tests;

[UnitTest]
public sealed class CalculateConsumptionTest
{
    readonly CalculateConsumptionDataSetFactory dataSetFactory = new();

    [Theory]
    [InlineData(Aggregation.Total,  new long[] { 1609538400 }, new long[] { 1609552799 }, new float[] { 3930f })]
    [InlineData(Aggregation.Actual, new long[] { 1609538400, 1609542000, 1609545600, 1609549200 }, new long[] { 1609541999, 1609545599, 1609549199, 1609552799 }, new float[] { 1234f, 242f, 654f, 1800f })]
    [InlineData(Aggregation.Hour,   new long[] { 1609538400, 1609542000, 1609545600, 1609549200 }, new long[] { 1609541999, 1609545599, 1609549199, 1609552799 }, new float[] { 1234f, 242f, 654f, 1800f })]
    [InlineData(Aggregation.Day,    new long[] { 1609538400, 1609545600 }, new long[] { 1609545599, 1609552799 }, new float[] { 1476f, 2454f })]
    [InlineData(Aggregation.Month,  new long[] { 1609538400 }, new long[] { 1609552799 }, new float[] { 3930f })]
    [InlineData(Aggregation.Year,   new long[] { 1609538400 }, new long[] { 1609552799 }, new float[] { 3930f })]
    public void Measurements_CalculateConsumption_Aggregation(Aggregation aggregation, long[] expectedDateFrom, long[] expectedDateTo, float[] expectedValues)
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
        Assert.Equal(expectedValues, result.Select(_ => _.Value));
        Assert.Equal(expectedDateFrom, result.Select(_ => _.DateFrom));
        Assert.Equal(expectedDateTo, result.Select(_ => _.DateTo));
    }
}
