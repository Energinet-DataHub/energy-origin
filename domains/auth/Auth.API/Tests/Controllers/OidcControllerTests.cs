using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using API.Controllers;
using API.Options;
using API.Services;
using API.Utilities;
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
                new("token_endpoint", tokenEndpoint.AbsoluteUri),
                new("end_session_endpoint", $"http://{oidcOptions.Value.AuthorityUri.Host}/connect/endsession")
            },
            KeySetUsing(tokenOptions.Value.PublicKeyPem)
        );

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var anyToken = TokenUsing(tokenOptions.Value);
        var userToken = TokenUsing(tokenOptions.Value, new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", $$"""{"access_token":"{{anyToken}}", "id_token":"{{anyToken}}", "userinfo_token":"{{userToken}}"}""");
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
        Assert.Contains("Authentication=", header[0]); // FIXME: verify cookie lifetime?
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenCodeIsMissing()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, null, null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"errorCode=2", query); // FIXME: codable error list?
    }

    [Fact]
    public async Task CallbackAsync_ShouldLogWarning_WhenCodeIsMissing()
    {
        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(accessor, cache, factory, mapper, service, issuer, oidcOptions, tokenOptions, logger, null, null, null);

        Mock.Get(logger).Verify(it => it.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    // FIXME: add tests for discovery failure

    // FIXME: add tests for code exchange failure

    // FIXME: add tests for invalid access, id and user token

    // FIXME: add tests for missing user token information

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

    private static string TokenUsing(TokenOptions options, Dictionary<string, object>? claims = default)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PrivateKeyPem));
        var key = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var descriptor = new SecurityTokenDescriptor()
        {
            Audience = options.Audience,
            Issuer = options.Issuer,
            SigningCredentials = credentials,
            Claims = claims
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }
}
