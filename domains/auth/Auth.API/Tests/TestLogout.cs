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
public sealed class TestLogout
{

    [Fact]
    public void Logout_success()
    {
        //Arrange
        var logger = new Mock<ILogger<AuthController>>();
        var oidcProvider = new Mock<IOidcProviders>();

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        var testToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3N1ZWQiOiIyMDIyLTA1LTE4VDEzOjEzOjQxLjY2MTI2NSswMDowMCIsImV4cGlyZXMiOiIyMDIyLTA1LTE5VDEzOjEzOjQxLjY2MDk4NyswMDowMCIsImFjdG9yIjoiYWN0b3IiLCJzdWJqZWN0Ijoic3ViamVjdCIsInNjb3BlIjpbInNjb3BlMSIsInNjb3BlMiJdfQ.W1C1xKiEYPDeuo1OfRFpm6L3j7YGQJTGmegIgLu2UIQ";
        var opaqueToken = "TestOpaqueToken";
        var expectedCookie = "Authorization=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";

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

        var AuthController = new AuthController(logger.Object, oidcProvider.Object, authOptionsMock.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        AuthController.HttpContext.Response.Cookies.Append("Authorization", $"{opaqueToken}", cookieOptions);
        AuthController.HttpContext.Request.Headers.Add("Authorization", "Bearer " + testToken);

        AuthController.Logout();

        //Assert
        Assert.Equal(expectedCookie, AuthController.HttpContext.Response.Headers.Values.Single());
    }
}
