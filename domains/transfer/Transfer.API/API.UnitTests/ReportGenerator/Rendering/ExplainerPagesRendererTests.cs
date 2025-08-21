using API.ReportGenerator.Rendering;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator.Rendering;

public class ExplainerPagesRendererTests
{
    [Fact]
    public async Task Render()
    {
        var sut = new ExplainerPagesRenderer();

        var html = sut.Render();

        await Verifier.Verify(html, extension: "html");
    }
}
