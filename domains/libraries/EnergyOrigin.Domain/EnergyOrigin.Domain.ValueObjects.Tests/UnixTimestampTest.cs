using Xunit;

namespace EnergyOrigin.Domain.ValueObjects.Tests;

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
    public void RoundToLatestHourExamples()
    {
        var now = DateTimeOffset.UtcNow;
        var latestHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Offset);
        Assert.Equal(UnixTimestamp.Create(latestHour), UnixTimestamp.Create(now).RoundToLatestHour());

        var alignedHour = new DateTimeOffset(2024, 2, 24, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(UnixTimestamp.Create(alignedHour), UnixTimestamp.Create(alignedHour).RoundToLatestHour());
    }

    [Fact]
    public void RoundToNextHourExamples()
    {
        var now = DateTimeOffset.UtcNow;
        var nextHour = new DateTimeOffset(now.Year, now.Month, now.Day, (now.Hour + 1) % 24, 0, 0, now.Offset);
        Assert.Equal(UnixTimestamp.Create(nextHour), UnixTimestamp.Create(now).RoundToNextHour());

        var alignedHour = new DateTimeOffset(2024, 2, 24, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(UnixTimestamp.Create(alignedHour), UnixTimestamp.Create(alignedHour).RoundToNextHour());
    }

    [Fact]
    public void RoundToLatestMidnight()
    {
        var now = DateTimeOffset.UtcNow;
        var latestMidnight = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(UnixTimestamp.Create(latestMidnight), UnixTimestamp.Create(now).RoundToLatestMidnight());
    }

    [Fact]
    public void MinutesToNextHourIsAlwaysLessThanAnHour()
    {
        var now = UnixTimestamp.Now();
        var timeUntilNextHour = now.TimeUntilNextHour();
        Assert.True(timeUntilNextHour.TotalSeconds >= 0);
        Assert.True(timeUntilNextHour.TotalSeconds <= 3600);
    }

    [Fact]
    public void MinutesToNextHourExamples()
    {
        var time = new DateTimeOffset(2023, 1, 1, 22, 44, 0, TimeSpan.Zero);
        var unixTimestamp = UnixTimestamp.Create(time);
        Assert.Equal(960, unixTimestamp.TimeUntilNextHour().TotalSeconds);

        time = new DateTimeOffset(2023, 1, 1, 22, 0, 0, TimeSpan.Zero);
        unixTimestamp = UnixTimestamp.Create(time);
        Assert.Equal(3600, unixTimestamp.TimeUntilNextHour().TotalSeconds);

        time = new DateTimeOffset(2023, 1, 1, 22, 0, 1, TimeSpan.Zero);
        unixTimestamp = UnixTimestamp.Create(time);
        Assert.Equal(3599, unixTimestamp.TimeUntilNextHour().TotalSeconds);

        time = new DateTimeOffset(2023, 1, 1, 22, 59, 0, TimeSpan.Zero);
        unixTimestamp = UnixTimestamp.Create(time);
        Assert.Equal(60, unixTimestamp.TimeUntilNextHour().TotalSeconds);
    }

    [Fact]
    public void GivenUnixTimestamp_WhenAddingMinutes_ResultIsTheSameWithAddTimespan()
    {
        int minutes = new Random().Next(-100, 100);
        var now = UnixTimestamp.Now();
        Assert.Equal(now.AddMinutes(minutes), now.Add(TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void GivenUnixTimestamp_WhenAddingHours_ResultIsTheSameWithAddTimespan()
    {
        int hours = new Random().Next(-100, 100);
        var now = UnixTimestamp.Now();
        Assert.Equal(now.AddHours(hours), now.Add(TimeSpan.FromHours(hours)));
    }

    [Fact]
    public void GivenUnixTimestamp_WhenAddingDays_ResultIsTheSameWithAddTimespan()
    {
        int days = new Random().Next(-100, 100);
        var now = UnixTimestamp.Now();
        Assert.Equal(now.AddDays(days), now.Add(TimeSpan.FromDays(days)));
    }

    [Fact]
    public void Max_ReturnsLargerTimestamp()
    {
        var past = UnixTimestamp.Create(1646312138);
        var present = UnixTimestamp.Create(1646312139);

        var result = UnixTimestamp.Max(past, present);

        Assert.Equal(present, result);
    }

    [Fact]
    public void Min_ReturnsSmallerTimestamp()
    {
        var past = UnixTimestamp.Create(1646312138);
        var present = UnixTimestamp.Create(1646312139);

        var result = UnixTimestamp.Min(past, present);

        Assert.Equal(past, result);
    }

    [Fact]
    public void GivenCurrentTimeStamp_WhenAdding1Year_ThenReturnTimestampWithAddedYear()
    {
        var now = UnixTimestamp.Create(1646312138);
        var future = now.AddYears(1);
        Assert.Equal(now.ToDateTimeOffset().AddYears(1), future.ToDateTimeOffset());
    }

    [Fact]
    public void GivenCurrentTimeStamp_WhenAdding4YearsWithOneLeapYear_ThenReturnCorrectTimestampWithAddedYears()
    {
        var now = UnixTimestamp.Create(1646312139);
        var future = 1772542539;
        Assert.Equal(now.ToDateTimeOffset().AddYears(4), DateTimeOffset.FromUnixTimeSeconds(future));
    }
}
