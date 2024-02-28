using System;
using System.Globalization;
using DataContext.ValueObjects;
using DotNet.Testcontainers.Configurations;
using Xunit;

namespace RegistryConnector.Worker.UnitTests.DataContext.ValueObjects;

public class UnixTimestampTest
{
    [Fact]
    public void Simple()
    {
        var t = UnixTimestamp.Create(1646312138);
        Assert.Equal(new DateTimeOffset(2022, 3, 3, 12, 55, 38, TimeSpan.Zero), t.ToDateTimeOffset());
    }

    [Fact]
    public void Inversion()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.Now.ToUnixTimeSeconds()); // Truncate to seconds
        Assert.Equal(now, UnixTimestamp.Create(now).ToDateTimeOffset());
    }

    [Fact]
    public void GreaterThan()
    {
        Assert.True(UnixTimestamp.Create(1) < UnixTimestamp.Create(2));
        Assert.True(UnixTimestamp.Create(3) > UnixTimestamp.Create(2));
        Assert.False(UnixTimestamp.Create(1) < UnixTimestamp.Create(1));
        Assert.False(UnixTimestamp.Create(1) > UnixTimestamp.Create(1));
    }

    [Fact]
    public void GreaterThanOrEqual()
    {
        Assert.True(UnixTimestamp.Create(1) <= UnixTimestamp.Create(1));
        Assert.True(UnixTimestamp.Create(1) <= UnixTimestamp.Create(2));
        Assert.True(UnixTimestamp.Create(3) >= UnixTimestamp.Create(1));
        Assert.True(UnixTimestamp.Create(1) >= UnixTimestamp.Create(1));
    }

    [Fact]
    public void RoundToLatestHour()
    {
        var now = DateTimeOffset.Now;
        var latestHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
        Assert.Equal(UnixTimestamp.Create(latestHour), UnixTimestamp.Create(now).RoundToLatestHour());
    }

    [Fact]
    public void RoundToLatestMidnight()
    {
        var now = DateTimeOffset.Now;
        var latestMidnight = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(UnixTimestamp.Create(latestMidnight), UnixTimestamp.Create(now).RoundToLatestMidnight());
    }

    [Fact]
    public void MinutesToNextHourIsAlwaysLessThanAnHour()
    {
        var now = UnixTimestamp.Now();
        var minutesToNextHour = now.MinutesUntilNextHour();
        Assert.True(minutesToNextHour >= 0);
        Assert.True(minutesToNextHour <= 60);
    }

    [Fact]
    public void MinutesToNextHourExamples()
    {
        var time = new DateTimeOffset(2023, 1, 1, 22, 44, 0, TimeSpan.Zero);
        var unixTimestamp = UnixTimestamp.Create(time);
        Assert.Equal(16, unixTimestamp.MinutesUntilNextHour());

        time = new DateTimeOffset(2023, 1, 1, 22, 0, 0, TimeSpan.Zero);
        unixTimestamp = UnixTimestamp.Create(time);
        Assert.Equal(60, unixTimestamp.MinutesUntilNextHour());

        time = new DateTimeOffset(2023, 1, 1, 22, 0, 1, TimeSpan.Zero);
        unixTimestamp = UnixTimestamp.Create(time);
        Assert.Equal(59, unixTimestamp.MinutesUntilNextHour());

        time = new DateTimeOffset(2023, 1, 1, 22, 59, 0, TimeSpan.Zero);
        unixTimestamp = UnixTimestamp.Create(time);
        Assert.Equal(1, unixTimestamp.MinutesUntilNextHour());
    }
}
