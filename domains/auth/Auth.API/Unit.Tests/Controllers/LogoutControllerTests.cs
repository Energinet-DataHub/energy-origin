using System.Security.Claims;
using System.Web;
using API.Controllers;
using API.Options;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class LogoutControllerTests
{
    private class TestableLogoutController : LogoutController
    {
        new public ClaimsPrincipal? User { get; set; }
    }
    private readonly TestableLogoutController controller = new();
    private readonly OidcOptions options;
    private readonly IMetrics metrics = Mock.Of<IMetrics>();
    private readonly ICryptography cryptography = Mock.Of<ICryptography>();
    private readonly ILogger<LogoutController> logger = Mock.Of<ILogger<LogoutController>>();
    private readonly string identityToken = Guid.NewGuid().ToString();
    // private readonly UserDescriptor descriptor;

    public LogoutControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        options = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;

        // var encryptedIdentityToken = Guid.NewGuid().ToString();
        // identityToken = Guid.NewGuid().ToString();

        // var cryptography = Mock.Of<ICryptography>();
        // Mock.Get(cryptography).Setup(it => it.Decrypt<string>(encryptedIdentityToken)).Returns(identityToken);

        // descriptor = new UserDescriptor(cryptography)
        // {
        //     EncryptedIdentityToken = encryptedIdentityToken
        // };
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        controller.User = TestClaimsPrincipal.Make();

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Mock.Of<IDiscoveryCache>();
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.AuthorityUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"post_logout_redirect_uri={options.FrontendRedirectUri.AbsoluteUri}", query);
        Assert.Contains($"id_token_hint={identityToken}", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRedirectWithHint_WhenInvokedAnonymously()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Mock.Of<IDiscoveryCache>();
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        var redirectResult = (RedirectResult)result;
        var uri = new Uri(redirectResult.Url);
        Assert.NotEqual(options.AuthorityUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.DoesNotContain($"id_token_hint", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRedirectToOverridenUri_WhenConfigured()
    {
        controller.User = TestClaimsPrincipal.Make();

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Mock.Of<IDiscoveryCache>();
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var redirectionUri = "http://redirection.r.us";

        var result = await controller.LogoutAsync(metrics, cache, cryptography, options, logger, redirectionUri);

        var redirectResult = (RedirectResult)result;
        var uri = new Uri(redirectResult.Url);
        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"post_logout_redirect_uri={redirectionUri}", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRedirectToOverridenUri_WhenConfiguredButNotAllowed()
    {
        controller.User = TestClaimsPrincipal.Make();

        var testOptions = TestOptions.Oidc(options, allowRedirection: false);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Mock.Of<IDiscoveryCache>();
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var redirectionUri = Guid.NewGuid().ToString();

        var result = await controller.LogoutAsync(metrics, cache, cryptography, testOptions, logger, redirectionUri);

        var redirectResult = (RedirectResult)result;
        var uri = new Uri(redirectResult.Url);
        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.DoesNotContain($"post_logout_redirect_uri={redirectionUri}", query);
        Assert.Contains($"post_logout_redirect_uri={testOptions.FrontendRedirectUri.AbsoluteUri}", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnRedirectToOurselves_WhenDiscoveryCacheFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.DoesNotContain($"{ErrorCode.QueryString}=", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldLogErrorMessage_WhenDiscoveryCacheFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        Mock.Get(logger).Verify(it => it.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LogoutAsync_ShouldCallMetricsLogout_WhenInvokedSuccessfully()
    {
        controller.User = TestClaimsPrincipal.Make();

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Mock.Of<IDiscoveryCache>();
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        _ = await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        Mock.Get(metrics).Verify(x => x.Logout(
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<ProviderType>()),
            Times.Once
        );
    }
}
