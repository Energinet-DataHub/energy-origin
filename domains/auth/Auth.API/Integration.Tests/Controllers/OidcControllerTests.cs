using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using API.Options;
using API.Values;
using IdentityModel;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tests.Integration;
using WireMock.Server;

namespace Integration.Tests.Controllers;

public class OidcControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public enum TokenInvalid
    {
        Identity,
        Access,
        User
    }

    public OidcControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    // [Fact]
    // public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithCookie_WhenInvoked()
    // {
    //     var server = WireMockServer.Start();

    //     var tokenOptions = factory.ServiceProvider.GetRequiredService<IOptions<TokenOptions>>();
    //     var oidcOptions = Options.Create(new OidcOptions()
    //     {
    //         AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
    //         ClientId = Guid.NewGuid().ToString(),
    //         AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug"),
    //         FrontendRedirectUri = new Uri("https://example-redirect.com")
    //     });
    //     var providerId = Guid.NewGuid().ToString();
    //     var name = Guid.NewGuid().ToString();
    //     var identityToken = TokenUsing(tokenOptions.Value, oidcOptions.Value.AuthorityUri.ToString(), oidcOptions.Value.ClientId);
    //     var accessToken = TokenUsing(tokenOptions.Value, oidcOptions.Value.AuthorityUri.ToString(), oidcOptions.Value.ClientId, claims: new() {
    //         { "scope", "something" },
    //     });
    //     var userToken = TokenUsing(tokenOptions.Value, oidcOptions.Value.AuthorityUri.ToString(), oidcOptions.Value.ClientId, claims: new() {
    //         { "mitid.uuid", providerId },
    //         { "mitid.identity_name", name }
    //     });

    //     server.MockConfigEndpoint()
    //         .MockJwksEndpoint(KeySetUsing(tokenOptions.Value.PublicKeyPem))
    //         .MockTokenEndpoint(accessToken, userToken, identityToken);

    //     var client = factory
    //         .CreateAnonymousClient(builder =>
    //             builder.ConfigureTestServices(services =>
    //                 services.AddScoped(x => oidcOptions)));

    //     var queryString = $"auth/oidc/callback?code={Guid.NewGuid()}";
    //     var result = await client.GetAsync(queryString);

    //     Assert.NotNull(result);
    //     Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
    //     Assert.Equal(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, result.Headers.Location?.AbsoluteUri);

    //     var header = result.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
    //     Assert.True(header.Any());
    //     Assert.Contains("Authentication=", header.First());
    //     Assert.Contains("; secure", header.First());
    //     Assert.Contains("; expires=", header.First());
    // }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenCodeExchangeFails()
    {
        var server = WireMockServer.Start();

        var tokenOptions = factory.ServiceProvider.GetRequiredService<IOptions<TokenOptions>>();
        var oidcOptions = Options.Create(new OidcOptions()
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            ClientId = Guid.NewGuid().ToString(),
            AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug"),
            FrontendRedirectUri = new Uri("https://example-redirect.com")
        });
        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions.Value, oidcOptions.Value.AuthorityUri.ToString(), oidcOptions.Value.ClientId);
        var accessToken = TokenUsing(tokenOptions.Value, oidcOptions.Value.AuthorityUri.ToString(), oidcOptions.Value.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions.Value, oidcOptions.Value.AuthorityUri.ToString(), oidcOptions.Value.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        server.MockConfigEndpoint().MockJwksEndpoint();

        var client = factory
            .CreateAnonymousClient(builder =>
                builder.ConfigureTestServices(services =>
                    services.AddScoped(x => oidcOptions)));

        var queryString = $"auth/oidc/callback?code={Guid.NewGuid()}";
        var result = await client.GetAsync(queryString);
        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.BadResponse}", query);
    }

    [Theory]
    [InlineData(TokenInvalid.Identity)]
    [InlineData(TokenInvalid.Access)]
    [InlineData(TokenInvalid.User)]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenTokenIsInvalid(TokenInvalid tokenInvalid)
    {
        var server = WireMockServer.Start();

        var correctIssuer = $"http://localhost:{server.Port}/op";
        var wrongIssuer = "http://example-wrong-issuer.com";

        var tokenOptions = factory.ServiceProvider.GetRequiredService<IOptions<TokenOptions>>();
        var oidcOptions = Options.Create(new OidcOptions()
        {
            AuthorityUri = new Uri(correctIssuer),
            ClientId = Guid.NewGuid().ToString(),
            AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug"),
            FrontendRedirectUri = new Uri("https://example-redirect.com")
        });
        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions.Value, tokenInvalid == TokenInvalid.Identity ? wrongIssuer : correctIssuer, oidcOptions.Value.ClientId);
        var accessToken = TokenUsing(tokenOptions.Value, tokenInvalid == TokenInvalid.Access ? wrongIssuer : correctIssuer, oidcOptions.Value.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions.Value, tokenInvalid == TokenInvalid.User ? wrongIssuer : correctIssuer, oidcOptions.Value.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        server.MockConfigEndpoint()
            .MockJwksEndpoint(KeySetUsing(tokenOptions.Value.PublicKeyPem))
            .MockTokenEndpoint(accessToken, userToken, identityToken);

        var client = factory
            .CreateAnonymousClient(builder =>
                builder.ConfigureTestServices(services =>
                    services.AddScoped(x => oidcOptions)));

        var queryString = $"auth/oidc/callback?code={Guid.NewGuid()}";
        var result = await client.GetAsync(queryString);
        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
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
