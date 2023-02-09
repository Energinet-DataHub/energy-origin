using System.Security.Claims;
using System.Web;
using API.Controllers;
using API.Options;
using API.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tests.Controllers;

public class LogoutControllerTests
{
    private readonly OidcOptions oidcOptions;
    private readonly IUserDescriptMapper mapper = Mock.Of<IUserDescriptMapper>();

    public LogoutControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        oidcOptions = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;
    }


    [Fact]
    public async Task GetAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var sentiel = Guid.NewGuid().ToString();

        var cryptography = Mock.Of<ICryptography>();
        Mock.Get(cryptography).Setup(it => it.Decrypt<string>("identityToken")).Returns(sentiel);

        var descriptor = new UserDescriptor(null, "", "", null, 0, false, "", "identityToken", cryptography);

        Mock.Get(mapper)
            .Setup(it => it.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(value: descriptor);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.Value.AuthorityUri.Host}/end_session") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LogoutController>>();

        var result = await new LogoutController().LogoutAsync(cache, mapper, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.Value.AuthorityUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"post_logout_redirect_uri={options.Value.FrontendRedirectUri.AbsoluteUri}", query);
        Assert.Contains($"id_token_hint={sentiel}", query);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnRedirectToOurselves_WhenDiscoveryCacheFails()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LogoutController>>();

        var result = await new LogoutController().LogoutAsync(cache, mapper, options, logger);

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
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LogoutController>>();

        var result = await new LogoutController().LogoutAsync(cache, mapper, options, logger);

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
