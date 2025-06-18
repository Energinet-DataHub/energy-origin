using API.ReportGenerator.Rendering;
using System.Threading.Tasks;
using API.ReportGenerator.Processing;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator.Rendering;

public class OtherCoverageRendererTests
{
    [Fact]
    public async Task Render()
    {
        var sut = new OtherCoverageRenderer();

        var values = new CoveragePercentage(50.5,60.1, 70, 80.9, null);

        var html = sut.Render(values);

        await Verifier.Verify(html, extension: "html");
    }
}
