using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
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
using NSubstitute.ReceivedExtensions;
using RichardSzalay.MockHttp;
using static API.Options.RoleConfiguration;
using JsonWebKeySet = IdentityModel.Jwk.JsonWebKeySet;

namespace Unit.Tests.Controllers;

public class OidcControllerTests
{
    private readonly OidcOptions oidcOptions;
    private readonly TokenOptions tokenOptions;
    private readonly IdentityProviderOptions providerOptions;
    private readonly RoleOptions roleOptions;
    private readonly ITokenIssuer issuer;
    private readonly ICryptography cryptography = Substitute.For<ICryptography>();
    private readonly IDiscoveryCache cache = Substitute.For<IDiscoveryCache>();
    private readonly IUserService service = Substitute.For<IUserService>();
    private readonly IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
    private readonly IUserProviderService userProviderService = Substitute.For<IUserProviderService>();
    private readonly IMetrics metrics = Substitute.For<IMetrics>();
    private readonly ILogger<OidcController> logger = Substitute.For<ILogger<OidcController>>();
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

    //Some soft of success :===============)
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
        cache.GetAsync().Returns(document);

        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: claims);

        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

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

    //TODO: HandleUserInfo - Error checks
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

        cache.GetAsync().Returns(document);

        var identityToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, document.Issuer, oidcOptions.ClientId, claims: claims);

        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");

        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        var query = HttpUtility.UrlDecode(new Uri(redirectResult.Url).Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
    }

    //TODO: Some sort of success ??
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

        cache.GetAsync().Returns(document);

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

        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var redirection = "https://goodguys.com";
        var oidcState = new OidcState(State: null, RedirectionUri: redirection, RedirectionPath: redirectionPath);

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, testOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null, oidcState.Encode());

        Assert.NotNull(action);
        var result = (RedirectResult)action;

        var uri = new Uri(result.Url);
        var redirectionUri = new Uri(redirection);
        Assert.Equal(redirectionUri.Host, uri.Host);
    }

    //TODO: Some sort of success ??
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

        cache.GetAsync().Returns(document);

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

        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var redirectionUri = "http://hackerz.com";
        var oidcState = new OidcState(State: null, RedirectionUri: redirectionUri, RedirectionPath: null);

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, testOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null, oidcState.Encode());

        Assert.NotNull(action);
        var result = (RedirectResult)action;

        var uri = new Uri(result.Url);
        Assert.NotEqual(new Uri(redirectionUri).Host, uri.Host);
        Assert.Equal(testOptions.FrontendRedirectUri.Host, uri.Host);
    }

    //TODO: Some sort of success ??
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

        cache.GetAsync().Returns(document);

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

        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var redirectionPath = "testpath1/testpath2";
        var oidcState = new OidcState(State: null, RedirectionUri: null, RedirectionPath: redirectionPath);

        var action = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, testOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null, oidcState.Encode());

        Assert.NotNull(action);
        var result = (RedirectResult)action;

        var uri = new Uri(result.Url);
        var map = QueryHelpers.ParseNullableQuery(uri.Query);
        Assert.NotNull(map);
        Assert.True(map.ContainsKey("redirectionPath"));
        Assert.Equal(redirectionPath, map["redirectionPath"]);
    }

     public static IEnumerable<object[]> RedirectionCheckParameters =>
        new List<object[]>
        {
            new object[] {null,true,"https://test.dk/"},
            new object[] {new OidcState(null, null, "/path/to/redirect"),false, "https://test.dk/?redirectionPath=path%2Fto%2Fredirect"},
            new object[] {new OidcState("someState", null, null), false, "https://test.dk/?state=someState"},
            new object[] {new OidcState("someState", "https://example.com/redirect", "/path/to/redirect"), false, "https://test.dk/?redirectionPath=path%2Fto%2Fredirect&state=someState"},
            new object[] {new OidcState("someState", "https://example.com/redirect", "/path/to/redirect"), true, "https://example.com/redirect?state=someState"},
            new object[] {new OidcState(null, "https://example.com/redirect", null), true, "https://example.com/redirect"}
        };

    [Theory]
    [MemberData(nameof(RedirectionCheckParameters))]
    public void RedirectionCheck_RerturnCorrectUri_WithDifferentParameters(OidcState? state, bool direction, string expectedUri)
    {
        var options = new OidcOptions(){AllowRedirection = direction, FrontendRedirectUri = new Uri("https://test.dk")};

        var result = OidcController.RedirectionCheck(options, state);

        Assert.Equal(expectedUri, result);
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
    public void CodeNullCheck_ShouldThrowAndReturnRedirectUrlWithErrorAndLogWarning_WhenGivenErrorConditions(string? error, string? errorDescription, string expected)
    {

        var result = Assert.Throws<OidcException>(() => OidcController.CodeNullCheck(null, logger, error, errorDescription, "https://example.com/login?errorCode=714")).Url;

        Assert.NotNull(result);
        Assert.Contains($"{ErrorCode.QueryString}={expected}", result);

        logger.Received(1).Log(
            Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>()
        );
    }

    [Fact]
    public void CodeNullCheck_ShouldNotThrow_WhenGivenCorrectConditions() => Assert.Null(Record.Exception(() => OidcController.CodeNullCheck("code", logger, null, null, "https://example.com/login?errorCode=714")));

    public static IEnumerable<object[]> WrongDiscoveryDocumentResponse =>
        new List<object[]>
        {
            new object[] {DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") })},
            new object[] {null}
        };

    [Theory]
    [MemberData(nameof(WrongDiscoveryDocumentResponse))]
    public void DiscoveryDocumentErrorChecks_ShouldThrowAndReturnRedirectUrlWithErrorAndLogWarning_WhenGivenErrorConditions(object? document)
    {
        var result = Assert.Throws<OidcException>(() => OidcController.DiscoveryDocumentErrorChecks((DiscoveryDocumentResponse?)document, logger, "https://test.dk")).Url;

        Assert.NotNull(result);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.DiscoveryUnavailable}", result);

        logger.Received(1).Log(
            Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void DiscoveryDocumentErrorChecks_ShouldNotThrowWhen_WhenGivenCorrectConditions() => Assert.Null(Record.Exception(() => OidcController.DiscoveryDocumentErrorChecks(DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", "https://test.dk") }), logger, "https://test.dk")));


    [Fact]
    public void GetClientAndResponse_ShouldThrowAndLogErrorAndReturnRedirectUrl_WhenRequestingTokenFails()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", tokenEndpoint.AbsoluteUri) });

        var result = Assert.ThrowsAsync<OidcException>(() => OidcController.GetClientAndResponse(factory, logger, oidcOptions, document, Guid.NewGuid().ToString(), "https://test.dk")).Result.Url;

        Assert.NotNull(result);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.BadResponse}", result);

        logger.Received(1).Log(
           Arg.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
           Arg.Any<EventId>(),
           Arg.Any<object>(),
           Arg.Any<Exception>(),
           Arg.Any<Func<object, Exception?, string>>());
    }

    //Test at der bliver kreeret en AuthorizationCodeTokenRequest med de korrekte values ??
    [Fact]
    public void GetClientAndResponse_ShouldReturnTokenResponse_WhenProvidedWithCorrectConditions()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.AuthorityUri.Host}/connect/token");
        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
            },
            KeySetUsing(tokenOptions.PublicKeyPem)
        );
        //Har behov alle 3 tokens men de kan bare være null???
        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{null}}", "id_token":"{{null}}", "userinfo_token":"{{null}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var result = OidcController.GetClientAndResponse(factory, logger, oidcOptions, document, Guid.NewGuid().ToString(), "https://example.com/login").Result;

        Assert.NotNull(result);
        Assert.IsType<TokenResponse>(result);
    }

    [Theory]
    [InlineData(null, "Wrong_Identity", "Wrong_UserInfo", typeof(ArgumentNullException))]
    [InlineData("", "Wrong_Identity", "Wrong_UserInfo", typeof(ArgumentException))]
    [InlineData("subject", "subject", "Wrong_UserInfo", typeof(SecurityTokenException))]
    [InlineData("subject", "Wrong_Identity", "subject", typeof(SecurityTokenException))]
    public void SubjectErrorCheck_ShouldThrow_WhenProvidedWrongConditions(string? subject, string identity, string userInfo, Type expectedException)
    {
        var identityClaim = new ClaimsPrincipal();
        identityClaim.AddIdentity(new ClaimsIdentity(new List<Claim>() { new(JwtRegisteredClaimNames.Sub, identity) }));
        var userInfoClaim = new ClaimsPrincipal();
        userInfoClaim.AddIdentity(new ClaimsIdentity(new List<Claim>() { new(JwtRegisteredClaimNames.Sub, userInfo) }));
        var result = Assert.Throws(expectedException, () => OidcController.SubjectErrorCheck(subject, identityClaim, userInfoClaim));
    }


    [Fact]
    public void SubjectErrorCheck_ShouldNotThrow_WhenProvidedCorrectConditions()
    {
        var identityClaim = new ClaimsPrincipal();
        identityClaim.AddIdentity(new ClaimsIdentity(new List<Claim>() { new(JwtRegisteredClaimNames.Sub, "subject") }));
        var userInfoClaim = new ClaimsPrincipal();
        userInfoClaim.AddIdentity(new ClaimsIdentity(new List<Claim>() { new(JwtRegisteredClaimNames.Sub, "subject") }));
        Assert.Null(Record.Exception(() => OidcController.SubjectErrorCheck("subject", identityClaim, userInfoClaim)));
    }

    [Fact]
    public void ProvidertypeIsFalseCheck_ShouldThrow_WhenProviderTypeIsNotInProviderOptions()
    {
        var newOptions = new IdentityProviderOptions(){ Providers = new List<ProviderType>(){ProviderType.MitIdPrivate, ProviderType.MitIdProfessional}};
        Assert.Throws<NotSupportedException>(() => OidcController.ProvidertypeIsFalseCheck(ProviderType.NemIdPrivate,newOptions));
    }
    [Fact]
    public void ProvidertypeIsFalseCheck_ShouldNotThrow_WhenProviderTypeIsInProviderOptions() => Assert.Null(Record.Exception(() => OidcController.ProvidertypeIsFalseCheck(ProviderType.NemIdPrivate,providerOptions)));

    [Theory]
    [InlineData(null, "TestName", "TestIdentity", typeof(ArgumentNullException))]
    [InlineData("", "TestName", "TestIdentity", typeof(ArgumentException))]
    [InlineData("TestProvider", null, "TestIdentity", typeof(ArgumentNullException))]
    [InlineData("TestProvider", "", "TestIdentity", typeof(ArgumentException))]
    [InlineData("TestProvider", "TestName", null, typeof(ArgumentNullException))]
    [InlineData("TestProvider", "TestName", "", typeof(ArgumentException))]
    public void ClaimsErrorCheck_ShouldThrow_WhenProvidedNullOrEmptyValues(string? scope, string? providerName, string? identityType, Type expectedException)
    {
        Assert.Throws(expectedException, () => OidcController.ClaimsErrorCheck(scope, providerName, identityType));
    }



    //TODO: MapUserDescriptor - ValidateToken errors
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

        cache.GetAsync().Returns(document);

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
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        var query = HttpUtility.UrlDecode(new Uri(redirectResult.Url).Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
    }

    [Theory]
    [InlineData(ProviderName.MitId, ProviderGroup.Private, ProviderType.MitIdPrivate)]
    [InlineData(ProviderName.MitIdProfessional, ProviderGroup.Professional, ProviderType.MitIdProfessional)]
    [InlineData(ProviderName.NemId, ProviderGroup.Private, ProviderType.NemIdPrivate)]
    [InlineData(ProviderName.NemId, ProviderGroup.Professional, ProviderType.NemIdProfessional)]
    public void GetIdentityProviderEnum_ShouldReturnCorrectProviderType_WhenProvidedWithVariousProviders(string providerName, string identity, ProviderType expected)
    {
        Assert.Equal(OidcController.GetIdentityProviderEnum(providerName,identity), expected);
    }

    [Fact]
    public void GetIdentityProviderEnum_ShouldThrow_WhenProvidedWithWrongProviders() => Assert.Throws<NotImplementedException>(() => OidcController.GetIdentityProviderEnum(ProviderName.MitIdProfessional, ProviderGroup.Private));

    public static IEnumerable<object[]> ValidUserInfoClaims =>
        new List<object[]>
        {
            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.cvr", Guid.NewGuid().ToString()),
                new("nemlogin.org_name", Guid.NewGuid().ToString()),
                new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, ("nemlogin.name","nemlogin.cvr", "nemlogin.org_name",
            new List<(ProviderKeyType, string)>(){(ProviderKeyType.Eia,"nemlogin.persistent_professional_id")})},

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.cvr", Guid.NewGuid().ToString()),
                new("nemlogin.org_name", Guid.NewGuid().ToString()),
                new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
                new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, ("nemlogin.name","nemlogin.cvr", "nemlogin.org_name",
            new List<(ProviderKeyType,string)>(){(ProviderKeyType.Eia, "nemlogin.persistent_professional_id"), (ProviderKeyType.Rid, "nemlogin.nemid.rid"), (ProviderKeyType.Rid, "nemlogin.cvr")})},

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("mitid.uuid", Guid.NewGuid().ToString()),
                new("mitid.identity_name", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdPrivate, ProviderGroup.Private, ("mitid.identity_name","NotACompany", "notACompany",
            new List<(ProviderKeyType, string)>(){(ProviderKeyType.MitIdUuid,"mitid.uuid")})},

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("mitid.uuid", Guid.NewGuid().ToString()),
                new("mitid.identity_name", Guid.NewGuid().ToString()),
                new("nemid.pid", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdPrivate, ProviderGroup.Private, ("mitid.identity_name","NotACompany", "notACompany",
            new List<(ProviderKeyType,string)>(){(ProviderKeyType.MitIdUuid,"mitid.uuid"), (ProviderKeyType.Pid, "nemid.pid")})},

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemid.common_name", Guid.NewGuid().ToString()),
                new("nemid.cvr", Guid.NewGuid().ToString()),
                new("nemid.company_name", Guid.NewGuid().ToString()),
                new("nemid.ssn", Guid.NewGuid().ToString()),
            }), ProviderType.NemIdProfessional, ProviderGroup.Professional, ("nemid.common_name","nemid.cvr", "nemid.company_name",
            new List<(ProviderKeyType, string)>(){(ProviderKeyType.Rid, "nemid.ssn")})},

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemid.common_name", Guid.NewGuid().ToString()),
                new("nemid.pid", Guid.NewGuid().ToString()),
            }), ProviderType.NemIdPrivate, ProviderGroup.Private, ("nemid.common_name", "NotACompany", "notACompany",
            new List<(ProviderKeyType, string)>(){(ProviderKeyType.Pid, "nemid.pid")})},
        };

    [Theory]
    [MemberData(nameof(ValidUserInfoClaims))]
    public void HandleUserInfo_ShouldReturnCorrectUserinfoAndNotThrow_WhenProvidedVariousParams(ClaimsIdentity claims, ProviderType providerType, string identityType, (string name, string tin, string companyName, List<(ProviderKeyType dictionaryKeys,string claimsKeys)> keyPairs) expected)
    {
        var userInfoClaim = new ClaimsPrincipal();
        userInfoClaim.AddIdentity(claims);

        var (name, tin, companyName, keys) = OidcController.HandleUserInfo(userInfoClaim, providerType, identityType);

        Assert.Equal(userInfoClaim.FindFirstValue(expected.name), name);
        Assert.Equal(userInfoClaim.FindFirstValue(expected.tin), tin);
        Assert.Equal(userInfoClaim.FindFirstValue(expected.companyName), companyName);

        foreach(var item in expected.keyPairs)
        {
            Assert.True(keys.TryGetValue(item.dictionaryKeys, out string? value));
            Assert.Contains(userInfoClaim.FindFirstValue(item.claimsKeys)!, value);
        }
    }

    public static IEnumerable<object[]> wrongUserClaims =>
        new List<object[]>
        {
            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.cvr", Guid.NewGuid().ToString()),
                new("nemlogin.org_name", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(KeyNotFoundException)},

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.org_name", Guid.NewGuid().ToString()),
                new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
                new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentNullException) },

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.cvr", Guid.NewGuid().ToString()),
                new("nemlogin.org_name",  string.Empty),
                new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
                new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.cvr", Guid.NewGuid().ToString()),
                new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
                new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentNullException) },

             new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemlogin.name", Guid.NewGuid().ToString()),
                new("nemlogin.cvr", string.Empty),
                new("nemlogin.org_name", Guid.NewGuid().ToString()),
                new("nemlogin.nemid.rid", Guid.NewGuid().ToString()),
                new("nemlogin.persistent_professional_id", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdProfessional, ProviderGroup.Professional, typeof(ArgumentException) },


            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("mitid.identity_name", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdPrivate, ProviderGroup.Private, typeof(KeyNotFoundException) },

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("mitid.uuid", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdPrivate, ProviderGroup.Private, typeof(ArgumentNullException) },

              new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("mitid.identity_name", string.Empty),
                new("mitid.uuid", Guid.NewGuid().ToString()),
            }), ProviderType.MitIdPrivate, ProviderGroup.Private, typeof(ArgumentException) },

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemid.common_name", Guid.NewGuid().ToString()),
                new("nemid.cvr", Guid.NewGuid().ToString()),
                new("nemid.company_name", Guid.NewGuid().ToString()),
            }), ProviderType.NemIdProfessional, ProviderGroup.Professional, typeof(KeyNotFoundException) },

            new object[] {new ClaimsIdentity(new List<Claim>
            {
                new("nemid.common_name", Guid.NewGuid().ToString()),
            }), ProviderType.NemIdPrivate, ProviderGroup.Private, typeof(KeyNotFoundException)},
        };
    [Theory]
    [MemberData(nameof(wrongUserClaims))]
    public void HandleUserInfo_ShouldThrow_WhenProvidedWithWrongParams(ClaimsIdentity claims, ProviderType providerType, string identityType, Type expectedException)
    {
        var userInfoClaim = new ClaimsPrincipal();
        userInfoClaim.AddIdentity(claims);

        Assert.Throws(expectedException, () => OidcController.HandleUserInfo(userInfoClaim, providerType, identityType));
    }

    [Fact]
    public async void HandleUserAsync_ShouldUpsertUser_WhenUserIsAlreadyKnown()
    {
        service.GetUserByIdAsync(Arg.Any<Guid?>()).Returns(new User
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 1 } },
            AllowCprLookup = true
        });
        userProviderService.FindUserProviderMatchAsync(Arg.Any<List<UserProvider>>()).Returns(new UserProvider());
        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(),Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var userProviders = new List<UserProvider>();

        var result = await OidcController.HandleUserAsync(service, userProviderService, userProviders, oidcOptions, "","","","","");

        Assert.NotNull(result);
        Assert.IsType<User>(result);

        await service.Received(1).UpsertUserAsync(Arg.Any<User>());
    }

    [Fact]
    public async void HandleUserAsync_ShouldCreateNewUserAndNotUpsertUser_WhenUserIsNull()
    {
        service.GetUserByIdAsync(Arg.Any<Guid?>()).Returns(null as User);
        userProviderService.FindUserProviderMatchAsync(Arg.Any<List<UserProvider>>()).Returns(new UserProvider());
        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(),Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        var userProviders = new List<UserProvider>();

        var result = await OidcController.HandleUserAsync(service, userProviderService, userProviders, oidcOptions, Guid.NewGuid().ToString(),Guid.NewGuid().ToString(),Guid.NewGuid().ToString(),Guid.NewGuid().ToString(),Guid.NewGuid().ToString());

        Assert.NotNull(result);
        Assert.IsType<User>(result);

        await service.Received(0).UpsertUserAsync(Arg.Any<User>());
    }

    public static IEnumerable<object[]> RoleMatcherData =>
        new List<object[]>
        {
             new object[] {
                new ClaimsIdentity(new List<Claim>{new("test1","TESTADMIN")}),
                new List<RoleConfiguration>{
                    new RoleConfiguration(){
                        Key = "TestRoleKeyAdmin",
                        Name = "TestAdmin",
                        Matches = new List<Match>(){
                            new Match(){
                                Property = "test1",
                                Value = "ADMIN",
                                Operator = "contains"},
                        }
                    },
                    new RoleConfiguration(){
                        Key = "TestRoleKeyAdmin2",
                        Name = "TestAdmin",
                        Matches = new List<Match>(){
                            new Match(){
                                Property = "test1",
                                Value = "TESTADMIN",
                                Operator = "equals"},
                        }
                    },
                    new RoleConfiguration(){
                        Key = "TestRoleKeyAdmin3",
                        Name = "TestAdmin",
                        Matches = new List<Match>(){
                            new Match(){
                                Property = "test1",
                                Value = "beep_boop_test",
                                Operator = "exists"},
                        }
                    },
                    new RoleConfiguration(){
                        Key = "TestRoleKeyAdmin4",
                        Name = "TestAdmin",
                        Matches = new List<Match>(){
                            new Match(){
                                Property = "test1",
                                Value = "boop_beep",
                                Operator = "equals"},
                            new Match(){
                                Property = "test1",
                                Value = "TEST",
                                Operator = "contains"},
                        }
                    },
                    new RoleConfiguration(){
                        Key = "WrongTestRoleKey",
                        Name = "TestAdmin",
                        Matches = new List<Match>(){
                            new Match(){
                                Property = "test1",
                                Value = "TESTADMIN",
                                Operator = "wrong_operator"},
                        }
                    },
                },
                new List<string>{"TestRoleKeyAdmin","TestRoleKeyAdmin2","TestRoleKeyAdmin3","TestRoleKeyAdmin4"}
            },
        };

    [Theory]
    [MemberData(nameof(RoleMatcherData))]
    public void CalculateMatchedRoles_ShouldDoSmth_WhenSmth(ClaimsIdentity identity, List<RoleConfiguration> roleConfigurations, List<string> expected)
    {
        var claims = new ClaimsPrincipal();
        claims.AddIdentity(identity);

        var options = new RoleOptions();
        options.RoleConfigurations.AddRange(roleConfigurations);

        var result = OidcController.CalculateMatchedRoles(claims, options);

        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    // [Fact]
    // public async Task GetUserDescriptor_DoesNotThrow_WhenOperationIsASuccess()
    // {
    //     var claims = new ClaimsPrincipal();
    //     OidcController.MapUserDescriptor(
    //         Arg.Any<ICryptography>(),
    //         Arg.Any<IUserProviderService>(),
    //         Arg.Any<IUserService>(),
    //         Arg.Any<IdentityProviderOptions>(),
    //         Arg.Any<OidcOptions>(),
    //         Arg.Any<RoleOptions>(),
    //         Arg.Any<DiscoveryDocumentResponse>(),
    //         Arg.Any<TokenResponse>()
    //         ).Returns((new UserDescriptor(claims), new TokenIssuer.UserData(1,1,new List<string>())));

    //     var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("test_key", "test_value") });

    //     var token = new TokenResponse();
    //     var result = await OidcController.GetUserDescriptor(logger,cryptography,userProviderService,service,providerOptions,oidcOptions,roleOptions,document,token,"https://test.com");
    // }

    //TODO: Er det en eller anden form for success scenarie?
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

        cache.GetAsync().Returns(document);

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
        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

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


    //TODO: Er det en eller anden form for success scenarie på metrics?
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

        cache.GetAsync().Returns(document);

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


        userProviderService.GetNonMatchingUserProviders(Arg.Any<List<UserProvider>>(), Arg.Any<List<UserProvider>>()).Returns(new List<UserProvider>());

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        factory.CreateClient(Arg.Any<string>()).Returns(http.ToHttpClient());

        await new OidcController().CallbackAsync(metrics, cache, factory, userProviderService, service, cryptography, issuer, oidcOptions, providerOptions, roleOptions, logger, Guid.NewGuid().ToString(), null, null);

        metrics.Received(1).Login(
            Arg.Any<Guid>(),
            Arg.Any<Guid?>(),
            Arg.Any<ProviderType>()
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
