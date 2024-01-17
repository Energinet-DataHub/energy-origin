using System;
using Xunit;
using API.Measurements.Helpers;
using FluentAssertions;

namespace Tests.Measurements.Helpers;

public class TimeSeriesHelperTests
{
    [Theory]
    [InlineData("2022-01-01T12:34:56.789+00:00", "2022-01-01T12:00:00.000+00:00")]
    [InlineData("2022-01-01T12:34:56.789+02:00", "2022-01-01T10:00:00.000+00:00")]
    public void ZeroedHour_GivenInput_ZeroOffsetToUtc(string inputTimestamp, string expectedTimestamp)
    {
        var actual = DateTimeOffset.Parse(inputTimestamp).ToUnixTimeSeconds().ZeroedHour();

        Assert.Equal(DateTimeOffset.Parse(expectedTimestamp), actual);
        Assert.Equal(TimeSpan.Zero, actual.Offset);
    }

    [Theory]
    [InlineData("2021-01-30", 2021, 1, 29)]
    public void ConvertDanishDateToDateTimeOffset_GivenInput_ReturnsCorrectDateTimeOffset(string inputDate,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var actual = inputDate.ConvertDanishDateToDateTimeOffset();

        expectedYear.Should().Be(actual.Year);
        expectedMonth.Should().Be(actual.Month);
        expectedDay.Should().Be(actual.Day);
        actual.Hour.Should().Be(23);
    }
}
