using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using API.Models.Entities;
using API.Options;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WireMock.Server;
using JsonWebKeySet = IdentityModel.Jwk.JsonWebKeySet;

namespace Integration.Tests.Controllers;

public class OidcControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    public enum TokenInvalid
    {
        Identity,
        Access,
        User
    }

    private readonly AuthWebApplicationFactory factory;

    public OidcControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontend_WhenInvoked()
    {
        var server = WireMockServer.Start();

        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var tokenOptions = factory.ServiceProvider.GetRequiredService<TokenOptions>();
        var oidcOptions = new OidcOptions
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            ClientId = Guid.NewGuid().ToString(),
            AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug"),
            FrontendRedirectUri = new Uri("https://example-redirect.com")
        };
        var roleOptions = new RoleOptions()
        {
            RoleConfigurations = new() {
                new() { Key = "exists", Name = "exists", Matches = new() { new() { Property = "mitid.uuid", Operator = "exists" } } },
                new() { Key = "equals", Name = "equals", Matches = new() { new() { Property = "mitid.uuid", Operator = "equals", Value = providerId } } },
                new() { Key = "contains", Name = "contains", Matches = new() { new() { Property = "mitid.uuid", Operator = "contains", Value = providerId.Substring(2, 10) } }, Inherits = new() { "inherited" } },
                new() { Key = "inherited", Name = "inherited" },
            }
        };
        var identityToken = TokenUsing(tokenOptions, oidcOptions.AuthorityUri.ToString(), oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, oidcOptions.AuthorityUri.ToString(), oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, oidcOptions.AuthorityUri.ToString(), oidcOptions.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name },
            { "idp", ProviderName.MitId },
            { "identity_type", ProviderGroup.Private }
        });

        server.MockConfigEndpoint()
            .MockJwksEndpoint(KeySetUsing(tokenOptions.PublicKeyPem))
            .MockTokenEndpoint(accessToken, userToken, identityToken);

        var client = factory.CreateAnonymousClient(builder => builder.ConfigureTestServices(services =>
        {
            services.AddScoped(_ => roleOptions);
            services.AddScoped(_ => oidcOptions);
        }));

        var queryString = $"auth/oidc/callback?code={Guid.NewGuid()}";
        var result = await client.GetAsync(queryString);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);

        Assert.NotNull(result.Headers.Location?.AbsoluteUri);
        var uri = new Uri(result.Headers.Location!.AbsoluteUri);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.NotNull(query);

        var values = HttpUtility.ParseQueryString(query);
        var token = values["token"];
        Assert.NotNull(token);

        var allRoles = roleOptions.RoleConfigurations.Select(x => x.Key);
        var roles = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Where(x => x.Type == UserClaimName.Roles).Select(x => x.Value).ToList();
        Assert.True(allRoles.All(x => roles.Contains(x)), "Roles in token does not contain all roles");
    }

    [Fact]
    public async Task CallbackAsync_ShouldUpdateUserProvidersOnUser_WhenInvokedAndUserExists()
    {
        var server = WireMockServer.Start();

        var tokenOptions = factory.ServiceProvider.GetRequiredService<TokenOptions>();
        var oidcOptions = new OidcOptions
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            ClientId = Guid.NewGuid().ToString(),
            AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug"),
            FrontendRedirectUri = new Uri("https://example-redirect.com")
        };

        var identityToken = TokenUsing(tokenOptions, oidcOptions.AuthorityUri.ToString(), oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, oidcOptions.AuthorityUri.ToString(), oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });

        var mitidUuid = Guid.NewGuid().ToString();
        var pid = Guid.NewGuid().ToString();

        var userToken = TokenUsing(tokenOptions, oidcOptions.AuthorityUri.ToString(), oidcOptions.ClientId, claims: new() {
            { "mitid.uuid", mitidUuid },
            { "mitid.identity_name", Guid.NewGuid().ToString() },
            { "nemid.pid", pid },
            { "idp", ProviderName.MitId },
            { "identity_type", ProviderGroup.Private }
        });

        server.MockConfigEndpoint()
            .MockJwksEndpoint(KeySetUsing(tokenOptions.PublicKeyPem))
            .MockTokenEndpoint(accessToken, userToken, identityToken);

        var user = await factory.AddUserToDatabaseAsync(new User
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = true,
            UserProviders = new List<UserProvider>
            {
                new()
                {
                    ProviderKeyType = ProviderKeyType.Pid,
                    UserProviderKey = pid
                }
            }
        });

        var client = factory.CreateAnonymousClient(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => oidcOptions)));

        var queryString = $"auth/oidc/callback?code={Guid.NewGuid()}";
        var result = await client.GetAsync(queryString);

        user = factory.DataContext.Users.Include(x => x.UserProviders).SingleOrDefault(x => x.Id == user.Id);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);

        Assert.NotNull(result.Headers.Location?.AbsoluteUri);
        var uri = new Uri(result.Headers.Location!.AbsoluteUri);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.Contains("token=", query);

        Assert.Equal(2, user!.UserProviders.Count);
        Assert.Contains(user.UserProviders, x => x.ProviderKeyType == ProviderKeyType.MitIdUuid && x.UserProviderKey == mitidUuid);
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenCodeExchangeFails()
    {
        var server = WireMockServer.Start();

        var oidcOptions = new OidcOptions
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            ClientId = Guid.NewGuid().ToString(),
            AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug"),
            FrontendRedirectUri = new Uri("https://example-redirect.com")
        };

        server.MockConfigEndpoint().MockJwksEndpoint();

        var client = factory.CreateAnonymousClient(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => oidcOptions)));

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

        var tokenOptions = factory.ServiceProvider.GetRequiredService<TokenOptions>();
        var oidcOptions = new OidcOptions
        {
            AuthorityUri = new Uri(correctIssuer),
            ClientId = Guid.NewGuid().ToString(),
            AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug"),
            FrontendRedirectUri = new Uri("https://example-redirect.com")
        };
        var providerId = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var identityToken = TokenUsing(tokenOptions, tokenInvalid == TokenInvalid.Identity ? wrongIssuer : correctIssuer, oidcOptions.ClientId);
        var accessToken = TokenUsing(tokenOptions, tokenInvalid == TokenInvalid.Access ? wrongIssuer : correctIssuer, oidcOptions.ClientId, claims: new() {
            { "scope", "something" },
        });
        var userToken = TokenUsing(tokenOptions, tokenInvalid == TokenInvalid.User ? wrongIssuer : correctIssuer, oidcOptions.ClientId, claims: new() {
            { "mitid.uuid", providerId },
            { "mitid.identity_name", name }
        });

        server.MockConfigEndpoint()
            .MockJwksEndpoint(KeySetUsing(tokenOptions.PublicKeyPem))
            .MockTokenEndpoint(accessToken, userToken, identityToken);

        var client = factory.CreateAnonymousClient(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => oidcOptions)));

        var queryString = $"auth/oidc/callback?code={Guid.NewGuid()}";
        var result = await client.GetAsync(queryString);
        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.Authentication.InvalidTokens}", query);
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
