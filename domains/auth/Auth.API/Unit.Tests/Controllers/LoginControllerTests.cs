using System.Web;
using API.Controllers;
using API.Options;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class LoginControllerTests
{
    private readonly OidcOptions oidcOptions;
    private readonly IdentityProviderOptions identityProviderOptions;
    private readonly ILogger<LoginController> logger = Substitute.For<ILogger<LoginController>>();
    private readonly IDiscoveryCache cache = Substitute.For<IDiscoveryCache>();

    public LoginControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        oidcOptions = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;
        identityProviderOptions = new IdentityProviderOptions()
        {
            Providers = new List<ProviderType>() { ProviderType.NemIdProfessional }
        };
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("authorization_endpoint", $"http://{options.AuthorityUri.Host}/connect") });

        cache.GetAsync().Returns(document);

        var result = await new LoginController().LoginAsync(cache, options, identityProviderOptions, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.AuthorityUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"state=", query);
        Assert.Contains($"prompt=login", query);
        Assert.Contains($"client_id={options.ClientId}", query);
        Assert.Contains($"redirect_uri={options.AuthorityCallbackUri.AbsoluteUri}", query);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToAuthorityWithConfiguredState_WhenInvokedWithConfigurations()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("authorization_endpoint", $"http://{options.AuthorityUri.Host}/connect") });

        cache.GetAsync().Returns(document);

        var state = Guid.NewGuid().ToString();
        var redirectionUri = Guid.NewGuid().ToString();
        var redirectionPath = Guid.NewGuid().ToString();

        var result = await new LoginController().LoginAsync(cache, options, identityProviderOptions, logger, state, redirectionUri, redirectionPath);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        var uri = new Uri(redirectResult.Url);
        var query = HttpUtility.UrlDecode(uri.Query);
        var map = QueryHelpers.ParseNullableQuery(query);

        Assert.NotNull(map);
        Assert.True(map.ContainsKey("state"));

        var decoded = OidcState.Decode(map["state"]);
        Assert.NotNull(decoded);
        Assert.Equal(state, decoded.State);
        Assert.Equal(redirectionUri, decoded.RedirectionUri);
        Assert.Equal(redirectionPath, decoded.RedirectionPath);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToOurselves_WhenDiscoveryCacheFails()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        cache.GetAsync().Returns(document);

        var result = await new LoginController().LoginAsync(cache, options, identityProviderOptions, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.DiscoveryUnavailable}", query);
    }

    [Fact]
    public async Task LoginAsync_ShouldLogErrorMessage_WhenDiscoveryCacheFails()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        cache.GetAsync().Returns(document);

        await new LoginController().LoginAsync(cache, options, identityProviderOptions, logger);


        logger.Received(1).Log(
            Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
