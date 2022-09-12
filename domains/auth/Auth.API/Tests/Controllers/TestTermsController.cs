using System.Threading.Tasks;
using API.Models;
using API.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Xunit.Categories;

namespace API.Controllers;

[UnitTest]
public class TestTermsController
{
    private readonly Mock<IPrivacyPolicyStorage> fakeStorage = new();

    private readonly TermsController termsController;

    public TestTermsController() => termsController = new(fakeStorage.Object);

    [Fact]
    public async Task Get_Terms_ReturnsPrivacyPolicy()
    {
        var fakePrivacyPolicy = new PrivacyPolicy(
            "<html>nice privacy</html>",
            "v1",
            "PrivacyPolicy"
        );
        fakeStorage
            .Setup(p => p.GetLatestVersion())
            .ReturnsAsync(() => fakePrivacyPolicy);

        var response = await termsController.Get();
        var result = response.Result as OkObjectResult;
        var privacyPolicy = result?.Value as PrivacyPolicy;

        Assert.Equal(fakePrivacyPolicy.Version, privacyPolicy?.Version);
        Assert.Equal(fakePrivacyPolicy.Terms, privacyPolicy?.Terms);
        Assert.Equal(fakePrivacyPolicy.Headline, privacyPolicy?.Headline);
    }
}
