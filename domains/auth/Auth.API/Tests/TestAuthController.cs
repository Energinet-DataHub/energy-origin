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
    [InlineData("foo", "Authorization=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/")]
    [InlineData("", "Authorization=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/")]
    public void Logout_delete_cookie_success(string testToken, string expectedExpiredCookie)
    {
        //Arrange
        var logger = new Mock<ILogger<AuthController>>();
        var oidcProvider = new Mock<IOidcProviders>();
        var tokenStorage = new Mock<ITokenStorage>();

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        var opaqueToken = "TestOpaqueToken";

        CookieOptions cookieOptions = new CookieOptions
        {
            Path = "/",
            Domain = "energioprindelse.dk",
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Expires = DateTime.UtcNow.AddHours(6),
        };

        //Act

        var AuthController = new AuthController(logger.Object, oidcProvider.Object, authOptionsMock.Object, tokenStorage.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        AuthController.HttpContext.Response.Cookies.Append(authOptionsMock.Object.Value.CookieName, $"{opaqueToken}", cookieOptions);
        AuthController.HttpContext.Request.Headers.Add(authOptionsMock.Object.Value.CookieName, "Bearer " + testToken);

        AuthController.Logout();

        //Assert
        Assert.Equal(expectedExpiredCookie, AuthController.HttpContext.Response.Headers.Values.Single());
    }
}
