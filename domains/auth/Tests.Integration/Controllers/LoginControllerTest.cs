using System;
using System.Net;
using System.Web;
using API.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Tests.Integration.LoginController;

public class LoginControllerTest : IClassFixture<LoginApiFactory>
{
    private readonly LoginApiFactory factory;
    public LoginControllerTest(LoginApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var client = factory.CreateUnauthenticatedClient();
        var result = await client.GetAsync("auth/login");
        var oidcOptions = factory.Services.CreateScope().ServiceProvider.GetRequiredService<IOptions<OidcOptions>>();        
        var query = HttpUtility.UrlDecode(result.Headers.Location.AbsoluteUri);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, result.StatusCode);        
        Assert.Contains($"client_id={oidcOptions.Value.ClientId}", query);
        Assert.Contains($"redirect_uri={oidcOptions.Value.AuthorityCallbackUri.AbsoluteUri}", query);
    }
}
