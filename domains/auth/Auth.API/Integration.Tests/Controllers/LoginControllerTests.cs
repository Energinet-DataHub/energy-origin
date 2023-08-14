using System.Net;
using System.Web;
using API.Options;
using API.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;

namespace Integration.Tests.Controllers;

public class LoginControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public LoginControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var server = WireMockServer.Start().MockConfigEndpoint().MockJwksEndpoint();

        var oidcOptions = new OidcOptions
        {
            AuthorityUri = new Uri($"http://localhost:{server.Port}/op"),
            ClientId = Guid.NewGuid().ToString(),
            AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug")
        };

        var client = factory.CreateAnonymousClient(builder => builder.ConfigureTestServices(services => services.AddScoped(_ => oidcOptions)));

        var (scope, arguments) = factory.ServiceProvider.GetRequiredService<IdentityProviderOptions>().GetIdentityProviderArguments();

        var result = await client.GetAsync("auth/login");
        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);

        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"client_id={oidcOptions.ClientId}", query);
        Assert.Contains($"redirect_uri={oidcOptions.AuthorityCallbackUri.AbsoluteUri}", query);
        Assert.Contains($"idp_values={arguments.First().Value}", query);
        Assert.Contains($"idp_params={arguments.Last().Value}", query);
        Assert.Contains($"scope={scope}", query);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnErrorCodeUrl_WhenDiscoveryCacheFails()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.GetAsync("auth/login");
        Assert.NotNull(result);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.DiscoveryUnavailable}", query);

        var oidcOptions = factory.ServiceProvider.GetRequiredService<OidcOptions>();
        var uri = new Uri(query!);
        Assert.Equal(oidcOptions.FrontendRedirectUri.Host, uri.Host);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
    }
}
