using System.Net;
using System.Web;
using API.Options;
using API.Values;
using Integration.Tests;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.Server;

namespace Tests.Integration.Controllers;

public class LogoutControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public LogoutControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var server = WireMockServer.Start().MockConfigEndpoint().MockJwksEndpoint();

        var identityToken = Guid.NewGuid().ToString();
        var user = await factory.AddUserToDatabaseAsync();
        var oidcOptions = Options.Create(new OidcOptions()
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            FrontendRedirectUri = new Uri("https://example.com")
        });

        var client = factory
            .CreateAuthenticatedClient(user, identityToken: identityToken, config: builder =>
                builder.ConfigureTestServices(services =>
                    services.AddScoped(x => oidcOptions)));

        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.Contains($"post_logout_redirect_uri={oidcOptions.Value.FrontendRedirectUri.AbsoluteUri}", query);
        Assert.Contains($"id_token_hint={identityToken}", query);

        var uri = new Uri(query!);
        Assert.Equal(oidcOptions.Value.AuthorityUri.Host, uri.Host);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnErrorCodeUrl_WhenDiscoveryCacheFails()
    {
        var oidcOptions = factory.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();
        var user = await factory.AddUserToDatabaseAsync();

        var client = factory.CreateAuthenticatedClient(user);

        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.DoesNotContain($"{ErrorCode.QueryString}=", query);

        var uri = new Uri(query!);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRedirectWithHint_WhenInvokedAnonymously()
    {
        var server = WireMockServer.Start().MockConfigEndpoint().MockJwksEndpoint();

        var oidcOptions = Options.Create(new OidcOptions()
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            FrontendRedirectUri = new Uri("https://example.com")
        });

        var client = factory
            .CreateAnonymousClient(config: builder =>
                builder.ConfigureTestServices(services =>
                    services.AddScoped(x => oidcOptions)));

        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"post_logout_redirect_uri={oidcOptions.Value.FrontendRedirectUri.AbsoluteUri}", query);
        Assert.DoesNotContain($"id_token_hint", query);
    }
}
