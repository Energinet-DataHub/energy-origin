using System.Net;
using System.Web;
using API.Options;
using IdentityModel.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Tests.Common;

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
        var identityToken = Guid.NewGuid().ToString();
        var oidcOptions = factory.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();
        var user = await factory.AddUserToDatabaseAsync();

        var client = await factory.CreateAuthenticatedClientAsync(user, identityToken: identityToken, config: builder =>
        {
            var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{oidcOptions.Value.AuthorityUri.Host}/end_session") });

            var cache = Mock.Of<IDiscoveryCache>();
            _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(x => cache);
            });
        });

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

        var client = await factory.CreateAuthenticatedClientAsync(user, config: builder =>
        {
            var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

            var cache = Mock.Of<IDiscoveryCache>();
            _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(x => cache);
            });
        });

        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.Contains($"errorCode=2", query);

        var uri = new Uri(query!);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotRedirectWithHint_WhenInvokedAnonymously()
    {
        var oidcOptions = factory.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();

        var client = factory.CreateUnauthenticatedClient(builder =>
        {
            var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("end_session_endpoint", $"http://{oidcOptions.Value.AuthorityUri.Host}/end_session") });

            var cache = Mock.Of<IDiscoveryCache>();
            _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(x => cache);
            });
        });

        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"post_logout_redirect_uri={oidcOptions.Value.FrontendRedirectUri.AbsoluteUri}", query);
        Assert.DoesNotContain($"id_token_hint", query);
    }
}
