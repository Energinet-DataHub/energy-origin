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
using IdentityModel.Jwk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Tests.Controllers;

public class OidcControllerTests
{
    private readonly OidcOptions oidcOptions;
    private readonly TokenOptions tokenOptions;
    private readonly IDiscoveryCache cache = Mock.Of<IDiscoveryCache>();
    private readonly IUserDescriptMapper mapper = Mock.Of<IUserDescriptMapper>();
    private readonly IUserService service = Mock.Of<IUserService>();
    private readonly ITokenIssuer issuer = Mock.Of<ITokenIssuer>();
    private readonly ILogger<OidcController> logger = Mock.Of<ILogger<OidcController>>();

    public OidcControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        oidcOptions = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;
        tokenOptions = configuration.GetSection(TokenOptions.Prefix).Get<TokenOptions>()!;
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithCookie_WhenInvoked()
    {
        var oidcOptions = TestOptions.Oidc(this.oidcOptions);
        var tokenOptions = TestOptions.Token(this.tokenOptions);

        var tokenEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/token");
        var userEndpoint = new Uri($"http://{oidcOptions.Value.AuthorityUri.Host}/connect/userinfo");

        var jwks = KeySetUsing(tokenOptions.Value.PublicKeyPem);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("token_endpoint", tokenEndpoint.AbsoluteUri), new("userinfo_endpoint", userEndpoint.AbsoluteUri) }, jwks);

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var http = new MockHttpMessageHandler();

        http.When(HttpMethod.Post, tokenEndpoint.AbsoluteUri).Respond("application/json", "{'access_token' : 'access_token', 'id_token' : 'id_token'}");
        http.When(HttpMethod.Post, userEndpoint.AbsoluteUri).Respond("application/json", "{'access_token' : 'access_token', 'id_token' : 'id_token'}");

        var result = await new OidcController().CallbackAsync(cache, http.ToHttpClient(), mapper, service, issuer, oidcOptions, logger, Guid.NewGuid().ToString(), null, null);

        Assert.Fail("Not done yet");
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenCodeIsMissing()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>());

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(cache, new MockHttpMessageHandler().ToHttpClient(), mapper, service, issuer, options, logger, null, null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.Value.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"errorCode=2", query); // FIXME: codable error list?
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

        var set = new JsonWebKeySet();
        set.Keys.Add(new JsonWebKey()
        {
            Kid = Base64Url.Encode(kid),
            Kty = "RSA",
            E = exponent,
            N = modulus
        });
        return set;
    }
}
