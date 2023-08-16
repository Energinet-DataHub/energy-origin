using System;
using API.GranularCertificateIssuer;
using CertificateValueObjects;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.GranularCertificateIssuer;

public class WalletPositionCalculatorTests
{
    [Theory]
    [InlineData("2022-01-01T00:00:00Z", 0)]
    [InlineData("2022-01-01T00:01:00Z", 1)]
    [InlineData("2022-01-01T01:00:00Z", 60)]
    [InlineData("2023-01-01T00:00:00Z", 525600)]
    [InlineData("6105-01-24T02:06:00Z", int.MaxValue - 1)]
    [InlineData("6105-01-24T02:07:00Z", int.MaxValue)]
    public void can_calculate_for_full_minutes(string start, int expectedPosition)
        => Calculate(start).Should().Be(expectedPosition);

    [Fact]
    public void no_result_when_over_max_date()
        => Calculate("6105-01-24T02:08:00Z").Should().BeNull();

    [Fact]
    public void no_result_when_before_start_date()
        => Calculate("2021-12-31T23:59:00Z").Should().BeNull();

    [Fact]
    public void no_result_when_not_a_full_minute()
        => Calculate("2022-01-01T00:00:01Z").Should().BeNull();

    private static int? Calculate(string start)
    {
        var s = DateTimeOffset.Parse(start);
        var e = s.AddHours(1);
        var period = new Period(s.ToUnixTimeSeconds(), e.ToUnixTimeSeconds());

        return WalletPositionCalculator.Calculate(period);
    }
}
