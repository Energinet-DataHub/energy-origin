using API.ReportGenerator.Rendering;
using System.Threading.Tasks;
using API.ReportGenerator.Processing;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator.Rendering;

public class OrganizationHeaderRendererTests
{
    [Fact]
    public async Task Render()
    {
        var sut = new OrganizationHeaderRenderer();

        var html = sut.Render("Wile E. Coyote & Sons, Inc", "12345678");

        await Verifier.Verify(html, extension: "html");
    }
}
