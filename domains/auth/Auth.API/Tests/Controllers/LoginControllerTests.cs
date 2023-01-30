using System.Web;
using API.Controllers;
using API.Options;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Controllers;

public class LoginControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var authorityHost = "them.com";
        var callbackUri = "us.com";
        var clientId = Guid.NewGuid().ToString();
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("authorization_endpoint", $"http://{authorityHost}/connect") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var options = Options.Create(new OidcOptions(AuthorityUrl: new Uri($"http://{authorityHost}/"), CacheDuration: new TimeSpan(6, 0, 0), ClientId: clientId, RedirectUri: callbackUri));

        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().GetAsync(cache, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(authorityHost, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"client_id={clientId}", query);
        Assert.Contains($"redirect_uri={callbackUri}", query);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRedirectToOurselves_WhenDiscoveryCacheFails()
    {
        var authorityHost = "them.com";
        var callbackHost = "us.com";
        var clientId = Guid.NewGuid().ToString();
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var options = Options.Create(new OidcOptions(AuthorityUrl: new Uri($"http://{authorityHost}/"), CacheDuration: new TimeSpan(6, 0, 0), ClientId: clientId, RedirectUri: $"http://{callbackHost}/"));

        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().GetAsync(cache, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(callbackHost, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"errorCode=2", query);
    }

    [Fact]
    public async Task GetAsync_ShouldLogErrorMessage_WhenDiscoveryCacheFails()
    {
        var authorityHost = "them.com";
        var callbackHost = "us.com";
        var clientId = Guid.NewGuid().ToString();
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var options = Options.Create(new OidcOptions(AuthorityUrl: new Uri($"http://{authorityHost}/"), CacheDuration: new TimeSpan(6, 0, 0), ClientId: clientId, RedirectUri: $"http://{callbackHost}/"));

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
