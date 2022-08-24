using API.Configuration;
using API.Controllers;
using API.Models;
using API.Services;
using API.TokenStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Categories;

namespace Tests.Controller;


[UnitTest]
public sealed class TestAuthController
{

    [Theory]
    [InlineData("Bearer foo")]
    [InlineData(null)]
    public void LogoutDeleteCookieReturnSuccess(string? testToken)
    {
        //Arrange
        var logger = new Mock<ILogger<AuthController>>();
        var oidcProvider = new Mock<IOidcProviders>();
        var tokenStorage = new Mock<ITokenStorage>();
        var cookies = new Mock<ICookies>();

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

        var authController = new AuthController(logger.Object, oidcProvider.Object, authOptionsMock.Object, tokenStorage.Object, cookies.Object)
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

    [Fact]
    public void ForwardAuth_OpaqueTokenAndInternalTokenExists_ReturnAuthorizationHeaderAndStatusCode200()
    {
        var logger = new Mock<ILogger<AuthController>>();
        var oidcProvider = new Mock<IOidcProviders>();
        var tokenStorage = new Mock<ITokenStorage>();
        tokenStorage.Setup(x => x.GetInteralTokenByOpaqueToken(It.IsAny<string>())).Returns(new InternalToken
        {
            Actor = "Actor",
            Subject = "Subject",
            Scope = new List<string> { "Scope1", "Scope2" },
            Issued = DateTime.UtcNow.AddHours(-1),
            Expires = DateTime.UtcNow.AddHours(6)
        });

        var cookies = new Mock<ICookies>();

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        var authController = new AuthController(logger.Object,
                                                oidcProvider.Object,
                                                authOptionsMock.Object,
                                                tokenStorage.Object,
                                                cookies.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var opaqueToken = "TestOpaqueToken";

        authController.HttpContext.Request.Headers.Add("Authorization", opaqueToken);

        var response = authController.ForwardAuth();

        Assert.Equal(200, ((StatusCodeResult)response).StatusCode);
    }

    [Fact]
    public void ForwardAuth_MissingOpaqueTokenInHeader_ReturnStatusCode401()
    {
        var logger = new Mock<ILogger<AuthController>>();
        var oidcProvider = new Mock<IOidcProviders>();
        var tokenStorage = new Mock<ITokenStorage>();
        var cookies = new Mock<ICookies>();

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        var authController = new AuthController(logger.Object, oidcProvider.Object, authOptionsMock.Object, tokenStorage.Object, cookies.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        authController.HttpContext.Request.Headers.Add("Authorization", "");

        var response = authController.ForwardAuth();

        Assert.Equal(401, ((StatusCodeResult)response).StatusCode);
    }

    [Fact]
    public void ForwardAuth_OpaqueTokenExistsInHeaderNoInternalToken_ReturnStatusCode401()
    {
        var logger = new Mock<ILogger<AuthController>>();
        var oidcProvider = new Mock<IOidcProviders>();
        var cookies = new Mock<ICookies>();

        var tokenStorage = new Mock<ITokenStorage>();
        tokenStorage.Setup(x => x.GetInteralTokenByOpaqueToken(It.IsAny<string>())).Returns((InternalToken?)null);

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        var authController = new AuthController(logger.Object, oidcProvider.Object, authOptionsMock.Object, tokenStorage.Object, cookies.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var opaqueToken = "TestOpaqueToken";

        authController.HttpContext.Request.Headers.Add("Authorization", opaqueToken);

        var response = authController.ForwardAuth();

        Assert.Equal(401, ((StatusCodeResult)response).StatusCode);
    }
}
