using API.Services;
using API.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Categories;
using Moq;
using API.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Tests;


[UnitTest]
public sealed class TestCookies
{

    [Fact]
    public void Create_Cookie_success()
    {
        //Arrange
        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
            CookieDomain = "energioprindelse.dk",
            CookieHttpOnly = "true",
            CookieSameSite = "Strict",
            CookieSecure = "true",
            CookieCreateExpires = 6, 
        });

        var cookieService = new CookieService(authOptionsMock.Object);
        var cookieOptions = cookieService.CreateCookieOptions(authOptionsMock.Object.Value.CookieCreateExpires);
        var logger = new Mock<ILogger<AuthController>>();
        var odieProvider = new Mock<IOidcProviders>();

        var eds = new AuthController(logger.Object, odieProvider.Object, cookieService, authOptionsMock.Object) { ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() }};

        var opaque_token = "sdsa";

        var tt = eds.TestLogin(opaque_token);

        var op = eds.HttpContext.Response.Headers.Values;



        Assert.NotNull(cookieOptions);
        Assert.IsType<string>(cookieOptions);

    }

 
}
