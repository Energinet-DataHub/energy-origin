using API.Models;
using API.Services;
using EnergyOriginDateTimeExtension;
using System;
using System.Linq;
using Xunit;

namespace Tests;
public sealed class ProductionAggregationTest
{
    readonly ProductionAggregationData dataSetFactory = new();

    [Theory]
    [InlineData(Aggregation.Total, new long[] { 1609538400 }, new long[] { 1609552799 }, new int[] { 3930 })]
    [InlineData(Aggregation.Actual, new long[] { 1609538400, 1609542000, 1609545600, 1609549200 }, new long[] { 1609541999, 1609545599, 1609549199, 1609552799 }, new int[] { 1234, 242, 654, 1800 })]
    [InlineData(Aggregation.Hour, new long[] { 1609538400, 1609542000, 1609545600, 1609549200 }, new long[] { 1609541999, 1609545599, 1609549199, 1609552799 }, new int[] { 1234, 242, 654, 1800 })]
    [InlineData(Aggregation.Day, new long[] { 1609538400, 1609545600 }, new long[] { 1609545599, 1609552799 }, new int[] { 1476, 2454 })]
    [InlineData(Aggregation.Month, new long[] { 1609538400 }, new long[] { 1609552799 }, new int[] { 3930 })]
    [InlineData(Aggregation.Year, new long[] { 1609538400 }, new long[] { 1609552799 }, new int[] { 3930 })]
    public void Measurements_CalculateAggregation(Aggregation aggregation, long[] expectedDateFrom, long[] expectedDateTo, int[] expectedValues)
    {
        // Arrange
        var dateFrom = new DateTime(2021, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2021, 1, 2, 1, 59, 59, DateTimeKind.Utc);
        var timeSeries = dataSetFactory.CreateTimeSeries();

        var aggregationCalculator = new MeasurementAggregation();

        // Act
        var result = aggregationCalculator.CalculateAggregation(
            timeSeries,
            dateFrom.ToUnixTime(),
            dateTo.ToUnixTime(),
            aggregation).AggregatedMeasurement.ToArray();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValues, result.Select(_ => _.Value));
        Assert.Equal(expectedDateFrom, result.Select(_ => _.DateFrom));
        Assert.Equal(expectedDateTo, result.Select(_ => _.DateTo));
    }
}
