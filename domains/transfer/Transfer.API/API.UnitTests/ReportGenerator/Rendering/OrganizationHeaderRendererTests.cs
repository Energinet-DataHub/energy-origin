using System.Threading.Tasks;
using API.ReportGenerator.Rendering;
using DataContext.Models;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator.Rendering;

public class OrganizationHeaderRendererTests
{
    [Theory]
    [InlineData(Language.Danish)]
    [InlineData(Language.English)]
    public Task HeaderHtml_ShouldMatchSnapshot(Language language)
    {
        var renderer = new OrganizationHeaderRenderer();
        var html = renderer.Render("Producent A/S", "11223344", language);
        return Verifier.Verify(html, extension: "html")
            .UseParameters(language)
            .UseDirectory("Snapshots");
    }
}
