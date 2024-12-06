using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementIntervalTest
{
    [Fact]
    public void TestOverlapsIncluded()
    {
        var now = UnixTimestamp.Now().RoundToLatestHour();
        MeasurementInterval m1 = MeasurementInterval.Create(now.AddHours(-2), now.AddHours(-1));
        MeasurementInterval m2 = MeasurementInterval.Create(now.AddHours(-3), now);

        m1.Overlaps(m2).Should().BeTrue();
        m2.Overlaps(m1).Should().BeTrue();
    }

    [Fact]
    public void TestOverlaps()
    {
        var now = UnixTimestamp.Now().RoundToLatestHour();
        MeasurementInterval m1 = MeasurementInterval.Create(now.AddHours(-4), now.AddHours(-2));
        MeasurementInterval m2 = MeasurementInterval.Create(now.AddHours(-3), now.AddHours(-1));

        m1.Overlaps(m2).Should().BeTrue();
        m2.Overlaps(m1).Should().BeTrue();
    }

    [Fact]
    public void TestNoOverlap()
    {
        var now = UnixTimestamp.Now().RoundToLatestHour();
        MeasurementInterval m1 = MeasurementInterval.Create(now.AddHours(-4), now.AddHours(-3));
        MeasurementInterval m2 = MeasurementInterval.Create(now.AddHours(-2), now.AddHours(-1));

        m1.Overlaps(m2).Should().BeFalse();
        m2.Overlaps(m1).Should().BeFalse();
    }

    [Fact]
    public void FindContaining()
    {
        var now = UnixTimestamp.Now().RoundToLatestHour();
        MeasurementInterval m1 = MeasurementInterval.Create(now.AddHours(-3), now.AddHours(-2));
        MeasurementInterval m2 = MeasurementInterval.Create(now.AddHours(-4), now.AddHours(-1));
        MeasurementInterval m3 = MeasurementInterval.Create(now.AddHours(-6), now.AddHours(-5));

        MeasurementInterval m4 = MeasurementInterval.Create(now.AddHours(-10), now.AddHours(-5));
        MeasurementInterval m5 = MeasurementInterval.Create(now.AddHours(-10), now.AddHours(-9));
        MeasurementInterval m6 = MeasurementInterval.Create(now.AddHours(-6), now.AddHours(-5));
        MeasurementInterval m7 = MeasurementInterval.Create(now.AddHours(-10), now.AddHours(-5));

        m1.FindFirstIntervalContaining([m2, m3]).Should().Be(m2);
        m2.FindFirstIntervalContaining([m1, m3]).Should().BeNull();
        m3.FindFirstIntervalContaining([m1, m2]).Should().BeNull();

        m5.FindFirstIntervalContaining([m4]).Should().Be(m4);
        m6.FindFirstIntervalContaining([m4]).Should().Be(m4);
        m7.FindFirstIntervalContaining([m4]).Should().Be(m4);
    }
}
