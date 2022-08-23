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
using System;

namespace Tests.Controller;


[UnitTest]
public sealed class TestAuthController
{

    [Theory]
    [InlineData("Bearer foo")]
    [InlineData(null)]
    public void Logout_delete_cookie_success(string? testToken)
    {
        //Arrange
        var logger = new Mock<ILogger<AuthController>>();
        var oidcProvider = new Mock<IOidcProviders>();
        var tokenStorage = new Mock<ITokenStorage>();
        var cookieService = new Mock<CookieService>();

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        var opaqueToken = "TestOpaqueToken";
        var expectedExpiredCookie = "Authorization=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";

        var notExpiredCookie = new CookieOptions
        {
            Path = "/",
            Domain = "energioprindelse.dk",
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Expires = DateTime.UtcNow.AddHours(6),
        };

        //Act

        var authController = new AuthController(logger.Object, oidcProvider.Object, authOptionsMock.Object, tokenStorage.Object, cookieService.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        authController.HttpContext.Response.Cookies.Append("Authorization", opaqueToken, notExpiredCookie);
        authController.HttpContext.Request.Headers.Add("Authorization", testToken);

        authController.Logout();

        //Assert
        Assert.Equal(expectedExpiredCookie, authController.HttpContext.Response.GetTypedHeaders().SetCookie.Single().ToString());

    }
}
