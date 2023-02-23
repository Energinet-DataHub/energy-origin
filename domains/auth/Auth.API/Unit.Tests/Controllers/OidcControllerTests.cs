using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using API.Controllers;
using API.Options;
using API.Services;
using API.Utilities;
using API.Values;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using RichardSzalay.MockHttp;

namespace Tests.Controllers;

public class OidcControllerTests
{
    private readonly IOptions<OidcOptions> oidcOptions;
    private readonly IOptions<TokenOptions> tokenOptions;
    private readonly ITokenIssuer issuer;
    private readonly IUserDescriptMapper mapper;
    private readonly IHttpContextAccessor accessor = Mock.Of<IHttpContextAccessor>();
    private readonly IDiscoveryCache cache = Mock.Of<IDiscoveryCache>();
    private readonly IUserService service = Mock.Of<IUserService>();
    private readonly IHttpClientFactory factory = Mock.Of<IHttpClientFactory>();
    private readonly ILogger<OidcController> logger = Mock.Of<ILogger<OidcController>>();
    private readonly MockHttpMessageHandler http = new();

    public OidcControllerTests()
    {
        IdentityModelEventSource.ShowPII = true;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        oidcOptions = Options.Create(configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!);
        tokenOptions = Options.Create(configuration.GetSection(TokenOptions.Prefix).Get<TokenOptions>()!);

        issuer = new TokenIssuer(Options.Create(configuration.GetSection(TermsOptions.Prefix).Get<TermsOptions>()!), tokenOptions, service);
        mapper = new UserDescriptMapper(
            new Cryptography(Options.Create(configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!)),
            Mock.Of<ILogger<UserDescriptMapper>>()
        );

        Mock.Get(accessor).Setup(it => it.HttpContext).Returns(new DefaultHttpContext());
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithCookie_WhenInvoked()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.Value.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.Value.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.Value.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId);
        var accessToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var action = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(action);
        Assert.IsType<OkObjectResult>(action);

        var result = action as OkObjectResult;
        var body = result!.Value as string;
        Assert.Contains("<html><head><meta ", body);
        Assert.Contains(" http-equiv=", body);
        Assert.Contains("refresh", body);
        Assert.Contains("<body />", body);
        Assert.Contains(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, body);

        Assert.NotNull(accessor.HttpContext);
        var header = accessor.HttpContext!.Response.Headers.SetCookie;
        Assert.True(header.Count >= 1);
        Assert.Contains("Authentication=", header[0]);
        Assert.Contains("; secure", header[0]);
        Assert.Contains("; expires=", header[0]);
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenDiscoveryFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.DiscoveryUnavailable}", query);
    }

    [Fact]
    public async Task CallbackAsync_ShouldLogError_WhenDiscoveryFails()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        _ = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

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
        var tokenEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", tokenEndpoint.AbsoluteUri) });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", """{"error":"it went all wrong"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.BadResponse}", query);
    }

    [Fact]
    public async Task CallbackAsync_ShouldLogError_WhenCodeExchangeFails()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", tokenEndpoint.AbsoluteUri) });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", """{"error":"it went all wrong"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        _ = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

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
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenUserTokenIsMissingName()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.Value.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.Value.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.Value.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId);
        var accessToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId, claims: new() {
            { "mitid.uuid", providerId },
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        var query = HttpUtility.UrlDecode(new Uri(redirectResult.Url).Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenUserTokenIsMissingId()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.Value.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.Value.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.Value.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId);
        var accessToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId, claims: new() {
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);
        var redirectResult = (RedirectResult)result;
        var query = HttpUtility.UrlDecode(new Uri(redirectResult.Url).Query);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenAccessTokenIsMissingScope()
    {
        var tokenEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", $"https://{oidcOptions.Value.AuthorityUri.Host}/op"),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.Value.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.Value.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId);
        var accessToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId);
        var userToken = TokenUsing(tokenOptions.Value, document.Issuer, oidcOptions.Value.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

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
        var tokenEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/token");

        var document = DiscoveryDocument.Load(
            new List<KeyValuePair<string, string>>() {
                new("issuer", expected),
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.Value.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.Value.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions.Value, identity, oidcOptions.Value.ClientId);
        var accessToken = TokenUsing(tokenOptions.Value, access, oidcOptions.Value.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions.Value, user, oidcOptions.Value.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{accessToken}}", "id_token":"{{identityToken}}", "userinfo_token":"{{userToken}}"}""");
        Mock.Get(factory).Setup(it => it.CreateClient(It.IsAny<string>())).Returns(http.ToHttpClient());

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, Guid.NewGuid().ToString(), null, null);

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
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, null, error, errorDescription);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);

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

    private static IdentityModel.Jwk.JsonWebKeySet KeySetUsing(byte[] pem)
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

        return new IdentityModel.Jwk.JsonWebKeySet
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

        var descriptor = new SecurityTokenDescriptor()
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
