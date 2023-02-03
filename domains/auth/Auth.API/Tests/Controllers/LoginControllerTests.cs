using System.Web;
using API.Controllers;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Tests.Controllers;

public class LoginControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var options = TestOptions.Oidc();

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("authorization_endpoint", $"http://{options.Value.AuthorityUri.Host}/connect") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().GetAsync(cache, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.Value.AuthorityUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"client_id={options.Value.ClientId}", query);
        Assert.Contains($"redirect_uri={options.Value.AuthorityCallbackUri.AbsoluteUri}", query);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRedirectToOurselves_WhenDiscoveryCacheFails()
    {
        var options = TestOptions.Oidc();

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().GetAsync(cache, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.Value.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"errorCode=2", query);
    }

    [Fact]
    public async Task GetAsync_ShouldLogErrorMessage_WhenDiscoveryCacheFails()
    {
        var options = TestOptions.Oidc();

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().GetAsync(cache, options, logger);

        Mock.Get(logger).Verify(it => it.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}
