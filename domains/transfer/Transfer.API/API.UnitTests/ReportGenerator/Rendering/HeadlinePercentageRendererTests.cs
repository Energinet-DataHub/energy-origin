using API.ReportGenerator.Rendering;
using System;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator.Rendering;

public class HeadlinePercentageRendererTests
{
    [Theory]
    [InlineData(0.1)]
    [InlineData(0.99)]
    public async Task Render_WhenPercentIsBetweenZeroAndOne_ShowLessThanOne(double percent)
    {
        var from = new DateTimeOffset(2025, 7, 18, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 8, 18, 0, 0, 0, TimeSpan.Zero);

        var sut = new HeadlinePercentageRenderer();

        var periodLabel = $"{from:dd.MM.yyyy} - {to.AddDays(-1):dd.MM.yyyy}";

        var html = sut.Render(percent, periodLabel);

        await Verifier.Verify(html, extension: "html");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.1)]
    [InlineData(99)]
    [InlineData(99.99)]
    [InlineData(100)]
    public async Task Render_WhenPercentIsGreaterThanOrEqualToOne_ShowValueWithoutDecimals(double percent)
    {
        var from = new DateTimeOffset(2025, 7, 18, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 8, 18, 0, 0, 0, TimeSpan.Zero);

        var sut = new HeadlinePercentageRenderer();

        var periodLabel = $"{from:dd.MM.yyyy} - {to.AddDays(-1):dd.MM.yyyy}";

        var html = sut.Render(percent, periodLabel);

        await Verifier.Verify(html, extension: "html");
    }
}
