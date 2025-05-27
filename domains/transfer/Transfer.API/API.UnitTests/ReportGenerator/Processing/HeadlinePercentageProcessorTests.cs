using System.Collections.Generic;
using API.ReportGenerator.Domain;
using API.ReportGenerator.Processing;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.ReportGenerator.Processing;

public class HeadlinePercentageProcessorTests
{
    [Fact]
    public void Render_ReturnsZero_WhenNoHoursProvided()
    {
        var processor = new HeadlinePercentageProcessor();
        var result = processor.Calculate(new List<HourlyEnergy>());
        result.Should().Be(0);
    }

    [Fact]
    public void Render_ReturnsHundredPercent_WhenTotalConsumptionIsZero()
    {
        var processor = new HeadlinePercentageProcessor();
        var hours = new List<HourlyEnergy>
        {
            new HourlyEnergy(0, 0, 0, 0, 0),
            new HourlyEnergy(1, 0, 0, 0, 0)
        };
        var result = processor.Calculate(hours);
        result.Should().Be(100);
    }

    [Fact]
    public void Render_ReturnsHundredPercent_WhenAllConsumptionIsMatched()
    {
        var processor = new HeadlinePercentageProcessor();
        var hours = new List<HourlyEnergy>
        {
            new HourlyEnergy(0, 10, 10, 0, 0),
            new HourlyEnergy(1, 20, 20, 0, 0)
        };
        var result = processor.Calculate(hours);
        result.Should().Be(100);
    }

    [Fact]
    public void Render_ReturnsCorrectPercentage_WhenPartialConsumptionIsMatched()
    {
        var processor = new HeadlinePercentageProcessor();
        var hours = new List<HourlyEnergy>
        {
            new HourlyEnergy(0, 10, 5, 5, 0),
            new HourlyEnergy(1, 20, 10, 10, 0)
        };
        var result = processor.Calculate(hours);
        result.Should().Be(50);
    }

    [Fact]
    public void Render_ReturnsZeroPercentage_WhenNoConsumptionIsMatched()
    {
        var processor = new HeadlinePercentageProcessor();
        var hours = new List<HourlyEnergy>
        {
            new HourlyEnergy(0, 10, 0, 10, 0),
            new HourlyEnergy(1, 20, 0, 20, 0)
        };
        var result = processor.Calculate(hours);
        result.Should().Be(0);
    }

    [Fact]
    public void Render_ReturnsApproximateFractionalCoverage()
    {
        var processor = new HeadlinePercentageProcessor();
        var hours = new List<HourlyEnergy>
        {
            new HourlyEnergy(0, 3, 1, 2, 0)
        };
        var result = processor.Calculate(hours);
        result.Should().BeApproximately(33.333, 1e-3);
    }

    [Fact]
    public void Render_IgnoresOvermatchedAndCalculatesCorrectEnergyCoverage()
    {
        var processor = new HeadlinePercentageProcessor();
        var hours = new List<HourlyEnergy>
        {
            new HourlyEnergy(0, 10, 8, 0, 2),
            new HourlyEnergy(1, 10, 5, 0, 5)
        };
        var result = processor.Calculate(hours);
        result.Should().Be(65);
    }

    [Fact]
    public void Render_ReturnsApproximateCoverageForMixedMultipleHours()
    {
        var processor = new HeadlinePercentageProcessor();
        var hours = new List<HourlyEnergy>
        {
            new HourlyEnergy(0, 5, 2, 3, 0),
            new HourlyEnergy(1, 10, 5, 5, 0),
            new HourlyEnergy(2, 15, 10, 5, 0)
        };
        var result = processor.Calculate(hours);
        result.Should().BeApproximately(56.6667, 1e-3);
    }
}
