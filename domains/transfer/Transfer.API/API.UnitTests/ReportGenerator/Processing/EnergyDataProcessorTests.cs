using System;
using System.Linq;
using API.ReportGenerator.Domain;
using API.ReportGenerator.Processing;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.ReportGenerator.Processing;

public class EnergyDataProcessorTests
{
    [Theory]
    [InlineData( 0,  10,  10, 10, 0)]
    [InlineData(12,  20,  20, 20, 0)]
    [InlineData( 6,  30,  10, 10, 20)]
    [InlineData(18,  25,  35, 25, 0)]
    public void Processor_Computes_MatchedBuckets(
        int hour, double cons, double prod,
        double expMatched, double expUnmatched)
    {
        var consumption = new[] { new DataPoint(new DateTime(2024, 1, 1, hour, 0, 0), cons) };
        var production  = new[] { new DataPoint(new DateTime(2024, 1, 1, hour, 0, 0), prod) };

        var result = EnergyDataProcessor
            .ToHourly(consumption, production)
            .First(h => h.Hour == hour);

        result.Matched  .Should().Be(expMatched);
        result.Unmatched.Should().Be(expUnmatched);
    }
}
