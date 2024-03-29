using System.Web;
using API.Controllers;
using API.Models.Dtos.Responses;
using API.Options;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class LogoutControllerTests
{
    private readonly LogoutController controller = new();
    private readonly OidcOptions options;
    private readonly IMetrics metrics = Substitute.For<IMetrics>();
    private readonly ICryptography cryptography;
    private readonly ILogger<LogoutController> logger = Substitute.For<ILogger<LogoutController>>();
    private readonly string identityToken = Guid.NewGuid().ToString();
    private readonly string encryptedIdentityToken;

    public LogoutControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        options = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;

        cryptography = new Cryptography(configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!);
        encryptedIdentityToken = cryptography.Encrypt(identityToken);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        controller.PrepareUser(encryptedIdentityToken: encryptedIdentityToken);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Substitute.For<IDiscoveryCache>();
        cache.GetAsync().Returns(document);

        var result = await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        Assert.NotNull(result);
        Assert.IsType<OkObjectResult>(result);
        var okObject = (OkObjectResult)result;
        var redirectResult = (RedirectUriResponse)okObject.Value!;

        var uri = new Uri(redirectResult.RedirectionUri);
        Assert.Equal(options.AuthorityUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"post_logout_redirect_uri={options.FrontendRedirectUri.AbsoluteUri}", query);
        Assert.Contains($"id_token_hint={identityToken}", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRedirectWithHint_WhenInvokedAnonymously()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Substitute.For<IDiscoveryCache>();
        cache.GetAsync().Returns(document);

        var result = await controller.LogoutAsync(metrics, cache, cryptography, options, logger);
        Assert.IsType<OkObjectResult>(result);
        var okObject = (OkObjectResult)result;
        var redirectResult = (RedirectUriResponse)okObject.Value!;
        var uri = new Uri(redirectResult.RedirectionUri);
        Assert.NotEqual(options.AuthorityUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.DoesNotContain($"id_token_hint", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRedirectToOverridenUri_WhenConfigured()
    {
        controller.PrepareUser(encryptedIdentityToken: encryptedIdentityToken);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Substitute.For<IDiscoveryCache>();
        cache.GetAsync().Returns(document);

        var redirectionUri = "http://redirection.r.us";

        var result = await controller.LogoutAsync(metrics, cache, cryptography, options, logger, redirectionUri);
        Assert.IsType<OkObjectResult>(result);
        var okObject = (OkObjectResult)result;
        var redirectResult = (RedirectUriResponse)okObject.Value!;
        var uri = new Uri(redirectResult.RedirectionUri);
        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"post_logout_redirect_uri={redirectionUri}", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRedirectToOverridenUri_WhenConfiguredButNotAllowed()
    {
        controller.PrepareUser(encryptedIdentityToken: encryptedIdentityToken);

        var testOptions = TestOptions.Oidc(options, allowRedirection: false);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Substitute.For<IDiscoveryCache>();
        cache.GetAsync().Returns(document);

        var redirectionUri = Guid.NewGuid().ToString();

        var result = await controller.LogoutAsync(metrics, cache, cryptography, testOptions, logger, redirectionUri);
        Assert.IsType<OkObjectResult>(result);
        var okObject = (OkObjectResult)result;
        var redirectResult = (RedirectUriResponse)okObject.Value!;
        var uri = new Uri(redirectResult.RedirectionUri);
        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.DoesNotContain($"post_logout_redirect_uri={redirectionUri}", query);
        Assert.Contains($"post_logout_redirect_uri={testOptions.FrontendRedirectUri.AbsoluteUri}", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnRedirectToOurselves_WhenDiscoveryCacheFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Substitute.For<IDiscoveryCache>();
        cache.GetAsync().Returns(document);

        var result = await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        Assert.NotNull(result);
        Assert.IsType<OkObjectResult>(result);
        var okObject = (OkObjectResult)result;
        var redirectResult = (RedirectUriResponse)okObject.Value!;

        var uri = new Uri(redirectResult.RedirectionUri);
        Assert.Equal(options.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.DoesNotContain($"{ErrorCode.QueryString}=", query);
    }

    [Fact]
    public async Task LogoutAsync_ShouldLogErrorMessage_WhenDiscoveryCacheFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Substitute.For<IDiscoveryCache>();
        cache.GetAsync().Returns(document);

        await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        logger.Received(1).Log(
            Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task LogoutAsync_ShouldCallMetricsLogout_WhenInvokedSuccessfully()
    {
        controller.PrepareUser(encryptedIdentityToken: encryptedIdentityToken);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{options.AuthorityUri.Host}/end_session") });

        var cache = Substitute.For<IDiscoveryCache>();
        cache.GetAsync().Returns(document);

        _ = await controller.LogoutAsync(metrics, cache, cryptography, options, logger);

        metrics.Received(1).Logout(
            Arg.Any<Guid>(),
            Arg.Any<Guid?>(),
            Arg.Any<ProviderType>()
        );
    }
}
