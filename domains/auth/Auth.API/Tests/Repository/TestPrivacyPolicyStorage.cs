using System.IO;
using System.Threading.Tasks;
using API.Configuration;
using API.Repository;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests.Repository;

public class TestPrivacyPolicyStorage
{
    private readonly Mock<IOptions<AuthOptions>> fakeAuthOptions = new();

    private readonly PrivacyPolicyStorage storage;

    public TestPrivacyPolicyStorage()
    {
        fakeAuthOptions.Setup(x => x.Value).Returns(new AuthOptions
        {
            TermsMarkdownFolder = Directory
                .GetParent(Directory.GetCurrentDirectory())?
                .Parent?
                .Parent?
                .FullName + "/resources"
        });

        storage = new PrivacyPolicyStorage(fakeAuthOptions.Object);
    }

    [Fact]
    public async Task Get_PrivacyPolicy_TermsReturnsAsHtml()
    {
        var privacyPolicy = await storage.GetLatestVersion();

        Assert.True(
            privacyPolicy?.Terms.Contains("<h1>MOCK PRIVATLIVSPOLITIK - ELOVERBLIK OG ENERGIOPRINDELSE</h1>")
        );
        Assert.Equal("v2", privacyPolicy?.Version);
    }
}
