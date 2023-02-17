using System.Net;
using System.Web;
using API.Options;
using IdentityModel.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Tests.Common;

namespace Tests.Integration.LoginController;

public class LoginControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public LoginControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var client = factory.CreateUnauthenticatedClient();
        var result = await client.GetAsync("auth/login");
        var oidcOptions = factory.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();
        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);

        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"client_id={oidcOptions.Value.ClientId}", query);
        Assert.Contains($"redirect_uri={oidcOptions.Value.AuthorityCallbackUri.AbsoluteUri}", query);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnErrorCodeUrl_WhenDiscoveryCacheFails()
    {
        var client = factory.CreateUnauthenticatedClient(builder =>
        {
            var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

            var cache = Mock.Of<IDiscoveryCache>();
            _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(x => cache);
            });
        });

        var result = await client.GetAsync("auth/login");
        Assert.NotNull(result);
        
        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.Contains($"errorCode=2", query);

        var oidcOptions = factory.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();
        var uri = new Uri(query!);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
    }
}
