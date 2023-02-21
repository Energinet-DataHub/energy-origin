using System.Net;
using System.Web;
using API.Options;
using API.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

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
        var broker = WireMockServer.Start();

        broker.Given(
            Request.Create().WithPath("/op/.well-known/openid-configuration").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBody(
                File.ReadAllText("./openid-configuration.json").Replace("https://pp.netseidbroker.dk", $"http://localhost:{broker.Port}")
            )
        );

        broker.Given(
            Request.Create().WithPath("/op/.well-known/openid-configuration/jwks").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBody(
                File.ReadAllText("./jwks.json")
            )
        );

        var oidcOptions = Options.Create(new OidcOptions()
        {
            AuthorityUri = new Uri($"http://localhost:{broker.Port}/op"),
            ClientId = "625fa04a-4b17-4727-8066-82cf5b5a8b0d",
            AuthorityCallbackUri = new Uri("https://oidcdebugger.com/debug")
        });

        var client = factory.CreateAnonymousClient(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(x => oidcOptions);
            });
        });

        var result = await client.GetAsync("auth/login");
        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);

        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"client_id={oidcOptions.Value.ClientId}", query);
        Assert.Contains($"redirect_uri={oidcOptions.Value.AuthorityCallbackUri.AbsoluteUri}", query);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnErrorCodeUrl_WhenDiscoveryCacheFails()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.GetAsync("auth/login");
        Assert.NotNull(result);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        Assert.Contains($"{ErrorCode.QueryString}={ErrorCode.AuthenticationUpstream.DiscoveryUnavailable}", query);

        var oidcOptions = factory.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();
        var uri = new Uri(query!);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
    }
}
