using System;
using CertificateValueObjects;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.CertificateValueObjects;

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
    public void Equals_WhenSameValues_ExpectTrue()
    {
        var dateFrom = 123L;
        var dateTo = 124L;
        var period1 = new Period(dateFrom, dateTo);
        var period2 = new Period(dateFrom, dateTo);

        period1.Equals(period2).Should().BeTrue();
    }
}
