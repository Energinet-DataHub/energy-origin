using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using RichardSzalay.MockHttp;
using JsonWebKeySet = IdentityModel.Jwk.JsonWebKeySet;

namespace Unit.Tests.Controllers;

public class OidcControllerTests
{
    private readonly OidcOptions oidcOptions;
    private readonly TokenOptions tokenOptions;
    private readonly IdentityProviderOptions providerOptions;
    private readonly RoleOptions roleOptions;
    private readonly ITokenIssuer issuer;
    private readonly ICryptography cryptography = Mock.Of<ICryptography>();
    private readonly IDiscoveryCache cache = Mock.Of<IDiscoveryCache>();
    private readonly IUserService service = Mock.Of<IUserService>();
    private readonly IHttpClientFactory factory = Mock.Of<IHttpClientFactory>();
    private readonly IUserProviderService userProviderService = Mock.Of<IUserProviderService>();
    private readonly IMetrics metrics = Mock.Of<IMetrics>();
    private readonly ILogger<OidcController> logger = Mock.Of<ILogger<OidcController>>();
    private readonly MockHttpMessageHandler http = new();

    public OidcControllerTests()
    {
        IdentityModelEventSource.ShowPII = true;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        oidcOptions = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;
        tokenOptions = configuration.GetSection(TokenOptions.Prefix).Get<TokenOptions>()!;
        providerOptions = configuration.GetSection(IdentityProviderOptions.Prefix).Get<IdentityProviderOptions>()!;
        roleOptions = configuration.GetSection(RoleOptions.Prefix).Get<RoleOptions>()!;

        issuer = new TokenIssuer(configuration.GetSection(TermsOptions.Prefix).Get<TermsOptions>()!, tokenOptions, configuration.GetSection(RoleOptions.Prefix).Get<RoleOptions>()!);
    }

    public static IEnumerable<object[]> ClaimsTestDataCorrect => new List<object[]>
    {
        new object[]
        {
            new Dictionary<string, object>
            {
                { "idp", ProviderName.MitId },
                { "identity_type", ProviderGroup.Private },
                { "mitid.uuid", Guid.NewGuid().ToString() },
                { "mitid.identity_name", Guid.NewGuid().ToString() }
            }
        },
        new object[]
        {
            new Dictionary<string, object>
            {
                { "idp", ProviderName.NemId },
                { "identity_type", ProviderGroup.Private },
                { "nemid.common_name", Guid.NewGuid().ToString() },
                { "nemid.pid", Guid.NewGuid().ToString() }
            }
        },
        new object[]
        {
            new Dictionary<string, object>
            {
                { "idp", ProviderName.NemId },
                { "identity_type", ProviderGroup.Professional },
                { "nemid.cvr", Guid.NewGuid().ToString() },
                { "nemid.ssn", Guid.NewGuid().ToString() },
                { "nemid.company_name", Guid.NewGuid().ToString() },
                { "nemid.common_name", Guid.NewGuid().ToString() }
            }
        },
        new object[]
        {
            new Dictionary<string, object>
            {
                { "idp", ProviderName.MitIdProfessional },
                { "identity_type", ProviderGroup.Professional },
                { "nemlogin.name", Guid.NewGuid().ToString() },
                { "nemlogin.cvr", Guid.NewGuid().ToString() },
                { "nemlogin.org_name", Guid.NewGuid().ToString() },
                { "nemlogin.nemid.rid", Guid.NewGuid().ToString() },
                { "nemlogin.persistent_professional_id", Guid.NewGuid().ToString() }
            }
        }
    };

    public static IEnumerable<object[]> ClaimsTestDataWrong => new List<object[]>
    {
        new object[]
        {
            new Dictionary<string, object>()
            {
                { "idp", ProviderName.MitId },
                { "identity_type", ProviderGroup.Private },
                { "mitid.identity_name", Guid.NewGuid().ToString() }
            }
        },
        new object[]
        {
            new Dictionary<string, object>()
            {
                { "idp", ProviderName.NemId },
                { "identity_type", ProviderGroup.Private },
                { "nemid.pid", Guid.NewGuid().ToString() }
            }
        },
        new object[]
        {
            new Dictionary<string, object>()
            {
                { "idp", ProviderName.NemId },
                { "identity_type", ProviderGroup.Professional },
                { "nemid.cvr", Guid.NewGuid().ToString() },
                { "nemid.company_name", Guid.NewGuid().ToString() },
                { "nemid.common_name", Guid.NewGuid().ToString() }
            }
        },
        new object[]
        {
            new Dictionary<string, object>()
            {
                { "idp", ProviderName.MitIdProfessional },
                { "identity_type", ProviderGroup.Professional },
                { "nemlogin.name", Guid.NewGuid().ToString() },
                { "nemlogin.cvr", Guid.NewGuid().ToString() },
                { "nemlogin.nemid.rid", Guid.NewGuid().ToString() },
                { "nemlogin.persistent_professional_id", Guid.NewGuid().ToString() }
            }
        },
        new object[]
        {
            new Dictionary<string, object>()
            {
                { "idp", "wrong" },
                { "identity_type", ProviderGroup.Private },
                { "mitid.uuid", Guid.NewGuid().ToString() },
                { "mitid.identity_name", Guid.NewGuid().ToString() }
            }
        },
        new object[]
        {
            new Dictionary<string, object>()
            {
                { "idp", ProviderName.MitId },
                { "identity_type", "wrong" },
                { "mitid.uuid", Guid.NewGuid().ToString() },
                { "mitid.identity_name", Guid.NewGuid().ToString() }
            }
        }
    };

    [Theory]
    [MemberData(nameof(ClaimsTestDataCorrect))]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontend_WhenInvokedWithVariousIdentityProviders(Dictionary<string, object> claims)
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: claims);

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(action);
        Assert.IsType<RedirectResult>(action);

        var result = (RedirectResult)action;
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);

        var uri = new Uri(result.Url);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        var map = QueryHelpers.ParseNullableQuery(uri.Query);
        Assert.NotNull(map);
        Assert.True(map.ContainsKey("token"));
    }

    [Theory]
    [MemberData(nameof(ClaimsTestDataWrong))]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenUserTokenArgumentsAreWrong(Dictionary<string, object> claims)
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: claims);

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        var query = HttpUtility.UrlDecode(new Uri(redirectResult.Url).Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/testpath")]
    public async Task CallbackAsync_ShouldReturnRedirectToOverridenUri_WhenConfigured(string? redirectionPath)
    {
        var testOptions = TestOptions.Oidc(oidcOptions, allowRedirection: true);
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{testOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{testOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name },
            { "idp", ProviderName.MitId  },
            { "identity_type", ProviderGroup.Private}
        });

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var redirection = "https://goodguys.com";
        var oidcState = new OidcState(State: null, RedirectionUri: redirection, RedirectionPath: redirectionPath);

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, testOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null, oidcState.Encode());

        Assert.NotNull(action);
        var result = (RedirectResult)action;

        var uri = new Uri(result.Url);
        var redirectionUri = new Uri(redirection);
        Assert.Equal(redirectionUri.Host, uri.Host);
    }

    [Fact]
    public async Task CallbackAsync_ShouldNotReturnRedirectToOverridenUri_WhenConfiguredButNotAllowed()
    {
        var testOptions = TestOptions.Oidc(oidcOptions, allowRedirection: false);

        var tokenEndpoint = new Uri($"http://{testOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{testOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{testOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name },
            { "idp", ProviderName.MitId  },
            { "identity_type", ProviderGroup.Private}
        });

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var redirectionUri = "http://hackerz.com";
        var oidcState = new OidcState(State: null, RedirectionUri: redirectionUri, RedirectionPath: null);

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, testOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null, oidcState.Encode());

        Assert.NotNull(action);
        var result = (RedirectResult)action;

        var uri = new Uri(result.Url);
        Assert.NotEqual(new Uri(redirectionUri).Host, uri.Host);
        Assert.Equal(testOptions.FrontendRedirectUri.Host, uri.Host);
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToPath_WhenConfigured()
    {
        var testOptions = TestOptions.Oidc(oidcOptions, allowRedirection: false);

        var tokenEndpoint = new Uri($"http://{testOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{testOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{testOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name },
            { "idp", ProviderName.MitId  },
            { "identity_type", ProviderGroup.Private}
        });

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var redirectionPath = "testpath1/testpath2";
        var oidcState = new OidcState(State: null, RedirectionUri: null, RedirectionPath: redirectionPath);

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, testOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null, oidcState.Encode());

        Assert.NotNull(action);
        var result = (RedirectResult)action;

        var uri = new Uri(result.Url);
        Assert.Equal(testOptions.FrontendRedirectUri.Host, uri.Host);
        Assert.Equal(testOptions.FrontendRedirectUri.AbsolutePath, uri.AbsolutePath);
        Assert.Contains($"redirectionPath={redirectionPath}", HttpUtility.UrlDecode(uri.AbsoluteUri));
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenDiscoveryFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.DiscoveryUnavailable}", query);
    }

    [Fact]
    public async Task CallbackAsync_ShouldLogError_WhenDiscoveryFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        _ = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

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
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenCodeExchangeFails()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", tokenEndpoint.AbsoluteUri) });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", """{"error":"it went all wrong"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.BadResponse}", query);
    }

    [Fact]
    public async Task CallbackAsync_ShouldLogError_WhenCodeExchangeFails()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", tokenEndpoint.AbsoluteUri) });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", """{"error":"it went all wrong"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        _ = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

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
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenUserTokenIsMissingId()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        var query = HttpUtility.UrlDecode(new Uri(redirectResult.Url).Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenAccessTokenIsMissingScope()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        var query = HttpUtility.UrlDecode(new Uri(redirectResult.Url).Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
    }

    [Theory]
    [InlineData("incorrect", "as expected", "as expected", "as expected")]
    [InlineData("as expected", "incorrect", "as expected", "as expected")]
    [InlineData("as expected", "as expected", "incorrect", "as expected")]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenTokenIsInvalid(string identity, string access, string user, string expected)
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", expected),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, identity, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, access, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, user, oidcOptions.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        var query = HttpUtility.UrlDecode(new Uri(redirectResult.Url).Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
    }

    [Theory]
    [InlineData("access_denied", "internal_error", ErrorCode.AuthenticationUpstream.InternalError)]
    [InlineData("access_denied", "user_aborted", ErrorCode.AuthenticationUpstream.Aborted)]
    [InlineData("access_denied", "private_to_business_user_aborted", ErrorCode.AuthenticationUpstream.Aborted)]
    [InlineData("access_denied", "no_ctx", ErrorCode.AuthenticationUpstream.NoContext)]
    [InlineData("access_denied", null, ErrorCode.AuthenticationUpstream.Failed)]
    [InlineData("invalid_request", null, ErrorCode.AuthenticationUpstream.InvalidRequest)]
    [InlineData("unauthorized_client", null, ErrorCode.AuthenticationUpstream.InvalidClient)]
    [InlineData("unsupported_response_type", null, ErrorCode.AuthenticationUpstream.InvalidRequest)]
    [InlineData("invalid_scope", null, ErrorCode.AuthenticationUpstream.InvalidScope)]
    [InlineData("server_error", null, ErrorCode.AuthenticationUpstream.InternalError)]
    [InlineData("temporarily_unavailable", null, ErrorCode.AuthenticationUpstream.InternalError)]
    [InlineData(null, null, ErrorCode.AuthenticationUpstream.Failed)]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithErrorAndLogWarning_WhenGivenErrorConditions(string? error, string? errorDescription, string expected)
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>());

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, null, error, errorDescription);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"{ErrorCode.QueryString}={expected}", query);

        Mock.Get(logger).Verify(it => it.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithErrorCode_WhenUsingProhibitedProvider()
    {
        var testProviderOptions = new IdentityProviderOptions
        {
            Providers = new List<ProviderType>()
        };
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, testProviderOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(action);
        Assert.IsType<RedirectResult>(action);

        var result = (RedirectResult)action;
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);

        var uri = new Uri(result.Url);
        Assert.Equal(oidcOptions.AuthorityUri.Host, uri.Host);

        var map = QueryHelpers.ParseNullableQuery(uri.Query);
        Assert.NotNull(map);
        Assert.True(map.ContainsKey("post_logout_redirect_uri"));

        uri = new Uri(map["post_logout_redirect_uri"]!);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        map = QueryHelpers.ParseNullableQuery(uri.Query);
        Assert.NotNull(map);
        Assert.True(map.ContainsKey("errorCode"));
    }

    [Fact]
    public async Task CallbackAsync_ShouldInvokeMapWithCorrectUserValues_WhenInvokedAsNemIdProfessional()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);
        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });

        var name = Guid.NewGuid().ToString();
        var tin = Guid.NewGuid().ToString();
        var companyName = Guid.NewGuid().ToString();
        var ssn = Guid.NewGuid().ToString();

        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "idp", ProviderName.NemId },
            { "identity_type", ProviderGroup.Professional },
            { "nemid.cvr", tin },
            { "nemid.ssn", ssn },
            { "nemid.company_name", companyName },
            { "nemid.common_name", name }
        });

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>() { new UserProvider() { ProviderKeyType = ProviderKeyType.Rid, UserProviderKey = ssn } });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(action);
        Assert.IsType<RedirectResult>(action);

        var result = (RedirectResult)action;
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);

        var uri = new Uri(result.Url);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        var map = QueryHelpers.ParseNullableQuery(uri.Query);
        Assert.NotNull(map);
        Assert.True(map.ContainsKey("token"));

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(map["token"]).Claims;
        Assert.Equal(name, claims.First(x => x.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal(tin, claims.First(x => x.Type == UserClaimName.Tin).Value);
        Assert.Equal(companyName, claims.First(x => x.Type == UserClaimName.OrganizationName).Value);
    }

    [Fact]
    public async Task CallbackAsync_ShouldInvokeUpsertUser_WhenInvokedWithExistingUser()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);
        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });

        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "idp", ProviderName.MitId },
            { "identity_type", ProviderGroup.Private },
            { "mitid.uuid", Guid.NewGuid().ToString() },
            { "mitid.identity_name", Guid.NewGuid().ToString() }
        });

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        Mock.Get(service)
            .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(value: new User
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 1 } },
                AllowCprLookup = true
            });

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(action);
        Assert.IsType<RedirectResult>(action);

        var result = (RedirectResult)action;
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);

        var uri = new Uri(result.Url);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        var map = QueryHelpers.ParseNullableQuery(uri.Query);
        Assert.NotNull(map);
        Assert.True(map.ContainsKey("token"));

        Mock.Get(service).Verify(x => x.UpsertUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Theory]
    [InlineData("0f2ffacc-3dd9-4e46-a446-dd632010e56b", true, true)]
    [InlineData("0f2ffacc-3dd9-4e46-a446-dd632010e56b", false, false)]
    [InlineData(null, false, true)]
    public async Task CallbackAsync_ShouldUseSubjectUserId_WhenConfigured(string? subject, bool matches, bool enabled)
    {
        var testOptions = TestOptions.Oidc(oidcOptions, reuseSubject: enabled);

        var tokenEndpoint = new Uri($"http://{testOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{testOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{testOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: subject);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: subject, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, testOptions.ClientId, subject: subject, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name },
            { "idp", ProviderName.MitId },
            { "identity_type", ProviderGroup.Private }
        });

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, testOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(action);
        Assert.IsType<RedirectResult>(action);

        var result = (RedirectResult)action;
        var map = QueryHelpers.ParseNullableQuery(new Uri(result.Url).Query);

        Assert.NotNull(map);
        Assert.True(map.ContainsKey("token"));

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(map["token"]).Claims.ToDictionary(x => x.Type, x => x);
        Assert.True(claims.ContainsKey(UserClaimName.Subject));
        Assert.Equal(matches, subject == claims[UserClaimName.Subject].Value);
    }

    [Fact]
    public async Task CallbackAsync_ShouldCallMetricsLogin_WhenInvokedSuccessfully()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );
        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "mitid.uuid", Guid.NewGuid().ToString() },
            { "mitid.identity_name", Guid.NewGuid().ToString() },
            { "idp", ProviderName.MitId  },
            { "identity_type", ProviderGroup.Private}
        });

        Mock.Get(userProviderService).Setup(it => it.GetNonMatchingUserProviders(It.IsAny<List<UserProvider>>(), It.IsAny<List<UserProvider>>())).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        _ = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Mock.Get(metrics).Verify(x => x.Login(
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<ProviderType>()),
            Times.Once
        );
    }

    private static JsonWebKeySet KeySetUsing(byte[] pem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(pem));
        var parameters = rsa.ExportParameters(false);

        var exponent = Base64Url.Encode(parameters.Exponent);
        var modulus = Base64Url.Encode(parameters.Modulus);
        var kid = SHA256.HashData(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(new Dictionary<string, string>() {
            {"e", exponent},
            {"kty", "RSA"},
            {"n", modulus}
        })));

        return new JsonWebKeySet
        {
            Keys = new() { new() {
                Kid = Base64Url.Encode(kid),
                Kty = "RSA",
                E = exponent,
                N = modulus
            }}
        };
    }

    private static string TokenUsing(TokenOptions tokenOptions, string issuer, string audience, string? subject = default, Dictionary<string, object>? claims = default)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(tokenOptions.PrivateKeyPem));
        var key = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var updatedClaims = claims ?? new Dictionary<string, object>();
        updatedClaims.Add("sub", subject ?? "subject");

        var descriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Issuer = issuer,
            SigningCredentials = credentials,
            Claims = updatedClaims
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }
}
