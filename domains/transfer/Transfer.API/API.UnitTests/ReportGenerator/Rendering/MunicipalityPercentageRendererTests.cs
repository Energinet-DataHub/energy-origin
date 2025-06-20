using System.Collections.Generic;
using System.Threading.Tasks;
using API.ReportGenerator.Domain;
using API.ReportGenerator.Rendering;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator.Rendering;

public class MunicipalityPercentageRendererTests
{
    [Fact]
    public async Task Render()
    {
        var municipalities = new List<MunicipalityDistribution>
        {
            new("101", 25),
            new("153", 10),
            new("155", 10),
            new("147", 20),
            new("157", 10),
            new(null, 15)
        };

        var sut = new MunicipalityPercentageRenderer();

        var html = sut.Render(municipalities);

        await Verifier.Verify(html, extension: "html");
    }

    [Fact]
    public async Task Render_WhenLessThan4AndIncludesNull_ExpectRendered()
    {
        var municipalities = new List<MunicipalityDistribution>
        {
            new("101", 25),
            new("151", 15),
            new(null, 41)
        };

        var sut = new MunicipalityPercentageRenderer();

        var html = sut.Render(municipalities);

        await Verifier.Verify(html, extension: "html");
    }
}
