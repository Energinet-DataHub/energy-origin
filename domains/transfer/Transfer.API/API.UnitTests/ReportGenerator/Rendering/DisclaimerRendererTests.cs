using System.Threading.Tasks;
using API.ReportGenerator.Rendering;
using DataContext.Models;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator.Rendering;

public class DisclaimerRendererTests
{
    [Theory]
    [InlineData(Language.Danish)]
    [InlineData(Language.English)]
    public async Task Render_ShouldMatchSnapshot(Language language)
    {
        var renderer = new DisclaimerRenderer();

        var html = renderer.Render(language);

        await Verifier.Verify(html, extension: "html")
            .UseParameters(language)
            .UseDirectory("Snapshots"); ;
    }
}
