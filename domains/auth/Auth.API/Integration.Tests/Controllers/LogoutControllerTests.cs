using System.Net;
using System.Text.Json;
using API.Models.Dtos.Responses;
using API.Options;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;

namespace Integration.Tests.Controllers;

public class LogoutControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public LogoutControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task LogoutAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var server = WireMockServer.Start().MockConfigEndpoint().MockJwksEndpoint();

        var identityToken = Guid.NewGuid().ToString();
        var user = await factory.AddUserToDatabaseAsync();
        var oidcOptions = new OidcOptions
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            FrontendRedirectUri = new Uri("https://example.com")
        };

        var client = factory.CreateAuthenticatedClient(user, identityToken: identityToken, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => oidcOptions)));

        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var resultContent = await result.Content.ReadAsStringAsync();
        var redirectUriResponse = JsonSerializer.Deserialize<RedirectUriResponse>(resultContent);
        var uri = new Uri(redirectUriResponse!.RedirectionUri);
        var map = QueryHelpers.ParseNullableQuery(uri.Query);
        Assert.NotNull(map);
        Assert.True(map.ContainsKey("post_logout_redirect_uri"));
        Assert.Equal(oidcOptions.FrontendRedirectUri.AbsoluteUri, map["post_logout_redirect_uri"]);
        Assert.True(map.ContainsKey("id_token_hint"));
        Assert.Equal(identityToken, map["id_token_hint"]);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnErrorCodeUrl_WhenDiscoveryCacheFails()
    {
        var oidcOptions = factory.ServiceProvider.GetRequiredService<OidcOptions>();
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user);

        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var resultContent = await result.Content.ReadAsStringAsync();
        var redirectUriResponse = JsonSerializer.Deserialize<RedirectUriResponse>(resultContent);
        Assert.Equal(oidcOptions.FrontendRedirectUri.AbsoluteUri, redirectUriResponse?.RedirectionUri);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task LogoutAsync_ReturnsOkResultWithRedirectionUri_WhenInvokedAnonymously()
    {
        var server = WireMockServer.Start().MockConfigEndpoint().MockJwksEndpoint();

        var oidcOptions = new OidcOptions
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            FrontendRedirectUri = new Uri("https://example.com")
        };

        var client = factory.CreateAnonymousClient(config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => oidcOptions)));
        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var resultContent = await result.Content.ReadAsStringAsync();
        var redirectUriResponse = JsonSerializer.Deserialize<RedirectUriResponse>(resultContent);

        Assert.Equal(oidcOptions.FrontendRedirectUri.AbsoluteUri, redirectUriResponse?.RedirectionUri);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
