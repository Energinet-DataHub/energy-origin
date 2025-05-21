using System.Linq;
using API.ReportGenerator.Domain;
using API.ReportGenerator.Processing;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.ReportGenerator.Processing;

public class HeadlinePercentageProcessorTests
{
    [Theory]
    [InlineData(0, 0,   0.0)]   // no hours
    [InlineData(1, 1, 100.0)]   // 1 of 1
    [InlineData(0, 1,   0.0)]   // 0 of 1
    [InlineData(1, 2,  50.0)]   // 1 of 2
    [InlineData(2, 5,  40.0)]   // 2 of 5
    [InlineData(5, 5, 100.0)]   // all
    public void Render_RepresentativeCases_YieldExpectedCoverage(
        int fullyMatched,
        int totalHours,
        double expectedPct)
    {
        var hours = Enumerable.Range(0, totalHours)
            .Select(i => new HourlyEnergy(
                i,
                0,
                i < fullyMatched ? 1 : 0,
                i < fullyMatched ? 0 : 1,
                0))
            .ToList();

        var processor = new HeadlinePercentageProcessor();

        var actual = processor.Render(hours);

        actual.Should().Be(expectedPct);
    }
}
