using System;
using System.Threading.Tasks;
using API.Helpers;
using API.Models;
using API.Repository;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Xunit.Categories;

namespace API.Controllers;

[UnitTest]
public class TestTermsController
{
    private readonly Mock<IPrivacyPolicyStorage> fakePrivacyPolicyStorage = new();

    private readonly TermsController termsController;

    public TestTermsController() => termsController = new(fakePrivacyPolicyStorage.Object);

    [Fact]
    public async Task Get_Terms_ReturnsPrivacyPolicy()
    {
        var fakePrivacyPolicy = new PrivacyPolicy(
            "<html>nice privacy</html>",
            "v1",
            "PrivacyPolicy"
        );
        fakePrivacyPolicyStorage
            .Setup(p => p.Get())
            .ReturnsAsync(() => fakePrivacyPolicy);

        var response = await termsController.Get();
        var result = response.Result as OkObjectResult;
        var privacyPolicy = result?.Value as PrivacyPolicy;

        Assert.Equal(fakePrivacyPolicy.Version, privacyPolicy?.Version);
        Assert.Equal(fakePrivacyPolicy.Terms, privacyPolicy?.Terms);
        Assert.Equal(fakePrivacyPolicy.Headline, privacyPolicy?.Headline);
    }
}
