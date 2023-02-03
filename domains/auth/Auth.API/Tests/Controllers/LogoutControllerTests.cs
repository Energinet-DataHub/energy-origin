using System.Web;
using API.Controllers;
using API.Options;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Controllers;

public class LogoutControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var authority = new Uri("http://them.com/");
        var callback = new Uri("http://us.com");
        var clientId = Guid.NewGuid().ToString();
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{authority.Host}/end_session") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var options = Options.Create(new OidcOptions(AuthorityUri: authority, CacheDuration: new TimeSpan(6, 0, 0), ClientId: clientId, AuthorityCallbackUri: new Uri("http://example.com"), FrontendRedirectUri: callback));

        var logger = Mock.Of<ILogger<LogoutController>>();

        var result = await new LogoutController().LogoutAsync(cache, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(authority.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"post_logout_redirect_uri={callback.AbsoluteUri}", query);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRedirectToOurselves_WhenDiscoveryCacheFails()
    {
        var authority = new Uri("http://them.com/");
        var callback = new Uri("http://us.com");
        var clientId = Guid.NewGuid().ToString();
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var options = Options.Create(new OidcOptions(AuthorityUri: authority, CacheDuration: new TimeSpan(6, 0, 0), ClientId: clientId, AuthorityCallbackUri: new Uri("http://example.com"), FrontendRedirectUri: callback));

        var logger = Mock.Of<ILogger<LogoutController>>();

        var result = await new LogoutController().LogoutAsync(cache, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(callback.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"errorCode=2", query);
    }

    [Fact]
    public async Task GetAsync_ShouldLogErrorMessage_WhenDiscoveryCacheFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var options = Options.Create(new OidcOptions(
            AuthorityUri: new Uri("http://them.com/"),
            CacheDuration: new TimeSpan(6, 0, 0),
            ClientId: "clientId",
            AuthorityCallbackUri: new Uri("http://us.com"),
            FrontendRedirectUri: new Uri("http://example.com")
        ));
        var logger = Mock.Of<ILogger<LogoutController>>();

        var result = await new LogoutController().LogoutAsync(cache, options, logger);

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
