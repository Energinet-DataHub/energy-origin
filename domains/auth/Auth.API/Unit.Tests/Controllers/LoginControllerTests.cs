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
using Microsoft.Extensions.Options;

namespace Unit.Tests.Controllers;

public class LoginControllerTests
{
    private readonly OidcOptions oidcOptions;
    private readonly IOptions<IdentityProviderOptions> identityProviderOptions;
    public LoginControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        oidcOptions = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;
        var identityProviderOption = new IdentityProviderOptions()
        {
            Providers = new List<ProviderType>() { ProviderType.NemID_Professional }
        };
        identityProviderOptions = Moptions.Create(identityProviderOption);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("authorization_endpoint", $"http://{options.Value.AuthorityUri.Host}/connect") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().LoginAsync(cache, options, identityProviderOptions, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.Value.AuthorityUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"state=", query);
        Assert.Contains($"client_id={options.Value.ClientId}", query);
        Assert.Contains($"redirect_uri={options.Value.AuthorityCallbackUri.AbsoluteUri}", query);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToAuthorityWithConfiguredState_WhenInvokedWithConfigurations()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("authorization_endpoint", $"http://{options.Value.AuthorityUri.Host}/connect") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LoginController>>();

        var state = Guid.NewGuid().ToString();
        var redirectionUri = Guid.NewGuid().ToString();

        var result = await new LoginController().LoginAsync(cache, options, identityProviderOptions, logger, state, redirectionUri);

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
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToOurselves_WhenDiscoveryCacheFails()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().LoginAsync(cache, options, identityProviderOptions, logger);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.Value.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.DiscoveryUnavailable}", query);
    }

    [Fact]
    public async Task LoginAsync_ShouldLogErrorMessage_WhenDiscoveryCacheFails()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().LoginAsync(cache, options, identityProviderOptions, logger);

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
