using System;
using System.Linq;
using API.Models;
using API.Services;
using Tests.Helpers;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class AggregationTests
{
    [Theory]
    [InlineData(Aggregation.Total, new long[] { 1609538400 }, new long[] { 1609552799 }, new long[] { 3930 })]
    [InlineData(Aggregation.Actual, new long[] { 1609538400, 1609542000, 1609545600, 1609549200 }, new long[] { 1609541999, 1609545599, 1609549199, 1609552799 }, new long[] { 1234, 242, 654, 1800 })]
    [InlineData(Aggregation.Hour, new long[] { 1609538400, 1609542000, 1609545600, 1609549200 }, new long[] { 1609541999, 1609545599, 1609549199, 1609552799 }, new long[] { 1234, 242, 654, 1800 })]
    [InlineData(Aggregation.Day, new long[] { 1609538400, 1609545600 }, new long[] { 1609545599, 1609552799 }, new long[] { 1476, 2454 })]
    [InlineData(Aggregation.Month, new long[] { 1609538400 }, new long[] { 1609552799 }, new long[] { 3930 })]
    [InlineData(Aggregation.Year, new long[] { 1609538400 }, new long[] { 1609552799 }, new long[] { 3930 })]
    public void Measurements_CalculateAggregation_ForConsumption(Aggregation aggregation, long[] expectedDateFrom, long[] expectedDateTo, long[] expectedValues)
    {
        // Arrange
        var dateFrom = new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2021, 1, 2, 1, 59, 59, TimeSpan.Zero);
        var timeSeries = MeasurementDataSet.CreateTimeSeries(type: MeterType.Consumption);
        var aggregationCalculator = new MeasurementAggregation();

        // Act
        var result = aggregationCalculator.CalculateAggregation(timeSeries, TimeZoneInfo.Utc, aggregation).AggregatedMeasurement.ToArray();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValues, result.Select(x => x.Value));
        Assert.Equal(expectedDateFrom, result.Select(x => x.DateFrom));
        Assert.Equal(expectedDateTo, result.Select(x => x.DateTo));
    }

    [Fact]
    public void Measurements_SupportLongValues()
    {
        // Arrange
        var timeSeries = MeasurementDataSet.CreateHugeValuesTimeSeries();
        var aggregationCalculator = new MeasurementAggregation();

        // Act
        var result = aggregationCalculator.CalculateAggregation(
            timeSeries,
            TimeZoneInfo.Utc,
            Aggregation.Total).AggregatedMeasurement.ToArray();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6000000000L, result.Single().Value);
    }

    [Theory]
    [InlineData(Aggregation.Total, new long[] { 1609538400 }, new long[] { 1609552799 }, new long[] { 3930 })]
    [InlineData(Aggregation.Actual, new long[] { 1609538400, 1609542000, 1609545600, 1609549200 }, new long[] { 1609541999, 1609545599, 1609549199, 1609552799 }, new long[] { 1234, 242, 654, 1800 })]
    [InlineData(Aggregation.Hour, new long[] { 1609538400, 1609542000, 1609545600, 1609549200 }, new long[] { 1609541999, 1609545599, 1609549199, 1609552799 }, new long[] { 1234, 242, 654, 1800 })]
    [InlineData(Aggregation.Day, new long[] { 1609538400, 1609545600 }, new long[] { 1609545599, 1609552799 }, new long[] { 1476, 2454 })]
    [InlineData(Aggregation.Month, new long[] { 1609538400 }, new long[] { 1609552799 }, new long[] { 3930 })]
    [InlineData(Aggregation.Year, new long[] { 1609538400 }, new long[] { 1609552799 }, new long[] { 3930 })]
    public void Measurements_CalculateAggregation_ForProduction(Aggregation aggregation, long[] expectedDateFrom, long[] expectedDateTo, long[] expectedValues)
    {
        // Arrange
        var dateFrom = new DateTimeOffset(2021, 1, 1, 22, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2021, 1, 2, 1, 59, 59, TimeSpan.Zero);
        var timeSeries = MeasurementDataSet.CreateTimeSeries(type: MeterType.Production);
        var aggregationCalculator = new MeasurementAggregation();

        // Act
        var result = aggregationCalculator.CalculateAggregation(timeSeries, TimeZoneInfo.Utc, aggregation).AggregatedMeasurement.ToArray();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValues, result.Select(x => x.Value));
        Assert.Equal(expectedDateFrom, result.Select(x => x.DateFrom));
        Assert.Equal(expectedDateTo, result.Select(x => x.DateTo));
    }

    [Theory]
    [InlineData(Aggregation.Day, 24, "Europe/Copenhagen")]
    [InlineData(Aggregation.Month, 31 * 24, "Europe/Copenhagen")]
    [InlineData(Aggregation.Day, 24, "America/Los_Angeles")]
    [InlineData(Aggregation.Month, 31 * 24, "America/Los_Angeles")]
    [InlineData(Aggregation.Day, 24, "Asia/Kolkata")]
    [InlineData(Aggregation.Month, 31 * 24, "Asia/Kolkata")]
    public void Measurements_CalculateAggregation_ForSpecificDates(Aggregation aggregation, int amount, string timeZoneId)
    {
        // Arrange
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var start = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        start = start.Add(-timeZone.GetUtcOffset(start.UtcDateTime));
        var timeSeries = MeasurementDataSet.CreateSpecificDateTimeSeries(startingAt: start, amount: amount);
        var aggregationCalculator = new MeasurementAggregation();

        // Act
        var result = aggregationCalculator.CalculateAggregation(timeSeries, timeZone, aggregation).AggregatedMeasurement.ToArray();
        var utcResult = aggregationCalculator.CalculateAggregation(timeSeries, TimeZoneInfo.Utc, aggregation).AggregatedMeasurement.ToArray();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(utcResult);

        Assert.Single(result);
        Assert.Equal(timeSeries.Sum(series => series.Measurements.Sum(x => x.Quantity)), result.First().Value);

        Assert.NotEqual(utcResult.Length, result.Length);
        Assert.Equal(utcResult.Sum(x => x.Value), result.First().Value);
    }
}
