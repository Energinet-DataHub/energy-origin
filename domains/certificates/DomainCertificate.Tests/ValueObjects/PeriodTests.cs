using System;
using DomainCertificate.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DomainCertificate.Tests.ValueObjects;

public class PeriodTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, 100)]
    public void Ctor_Success(long dateFromSeconds, long dateToSeconds)
    {
        var period = new Period(dateFromSeconds, dateToSeconds);

        period.DateFrom.Should().Be(dateFromSeconds);
        period.DateTo.Should().Be(dateToSeconds);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(200, 1)]
    public void Ctor_Fail(long dateFromSeconds, long dateToSeconds)
    {
        var createPeriod = () => new Period(dateFromSeconds, dateToSeconds);
        createPeriod.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToDateInterval()
    {
        var threeMinAnd20Seconds = new TimeSpan(0, 3, 20);
        var period = new Period(DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.AddSeconds(200).ToUnixTimeSeconds());

        var dateInterval = period.ToDateInterval();

        dateInterval.Duration.Should().Be(threeMinAnd20Seconds);
    }
}
