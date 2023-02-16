using System.Net;
using System.Web;
using API.Options;
using API.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using API.Models.Entities;
using API.Repositories.Data;
using Microsoft.EntityFrameworkCore;

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
        var accessToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();

        var user = new User()
        {
            ProviderId = Guid.NewGuid().ToString(),
            Name = "johnny",
            AcceptedTermsVersion = 2,
            Tin = null,
            AllowCPRLookup = true
        };

        var dbContext = factory.ServiceProvider.GetRequiredService<DataContext>();
        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var hehe = dbContext.Users.ToList();

        var client = await factory.CreateAuthenticatedClientAsync(user, accessToken, identityToken);

        var result = await client.GetAsync("auth/logout");
        Assert.NotNull(result);

        var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
        var oidcOptions = factory.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();
        var uri = new Uri(query!);
        Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
        Assert.Contains($"post_logout_redirect_uri={oidcOptions.Value.FrontendRedirectUri.AbsoluteUri}", query);
        Assert.Contains($"id_token_hint={identityToken}", query);
    }

    //[Fact]
    //public async Task LogoutAsync_ShouldReturnErrorCodeUrl_WhenDiscoveryCacheFails()
    //{
    //    var client = factory.CreateUnauthenticatedClient(builder =>
    //    {
    //        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("error", "it went all wrong") });

    //        var cache = Mock.Of<IDiscoveryCache>();
    //        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

    //        builder.ConfigureTestServices(services =>
    //        {
    //            services.AddScoped(x => Mock.Of<IDiscoveryCache>());
    //            services.AddScoped(x => Mock.Of<IUserDescriptMapper>());
    //        });
    //    });

    //    var result = await client.GetAsync("auth/logout");
    //    Assert.NotNull(result);

    //    var query = HttpUtility.UrlDecode(result.Headers.Location?.AbsoluteUri);
    //    Assert.Contains($"errorCode=2", query);

    //    var oidcOptions = factory.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();
    //    var uri = new Uri(query!);
    //    Assert.Equal(oidcOptions.Value.FrontendRedirectUri.Host, uri.Host);
    //    Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);
    //}

}
