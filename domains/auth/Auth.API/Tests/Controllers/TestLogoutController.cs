using System;
using System.Linq;
using API.Configuration;
using API.Services;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace API.Controllers;

public class TestLogoutController
{
    private readonly Mock<IOidcService> mockSignaturGruppen = new();
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ITokenStorage> tokenStorage = new();

    private LogoutController logoutController;

    public TestLogoutController()
    {
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        logoutController = new LogoutController(
            tokenStorage.Object,
            authOptionsMock.Object,
            mockSignaturGruppen.Object
        )
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    public static IEnumerable<object[]> Cookie => new[]
    {
        new object[] { $"Authorization=TestOpaqueToken;Path=/;Domain = energioprindelse.dk;HttpOnly = true; SameSite = SameSiteMode.Strict,Secure = true;Expires = {DateTime.UtcNow.AddHours(6)}" },
        new object[] { null },
    };

    [Theory, MemberData(nameof(Cookie))]
    public void LogoutDeleteCookieReturnSuccess(string? TestCookie)
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

        var expectedExpiredCookie = "Authorization=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";

        //Act

        var authController = new AuthController(logger.Object, oidcProvider.Object, authOptionsMock.Object, tokenStorage.Object, cookies.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        authController.HttpContext.Request.Headers.Add("Cookie", TestCookie);

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
        var cookies = new Mock<ICookies>();
        var authOptionsMock = new Mock<IOptions<AuthOptions>>();

        tokenStorage.Setup(x => x.GetInteralTokenByOpaqueToken(It.IsAny<string>())).Returns(new InternalToken
        {
            Actor = "Actor",
            Subject = "Subject",
            Scope = new List<string> { "Scope1", "Scope2" },
            Issued = DateTime.UtcNow.AddHours(-1),
            Expires = DateTime.UtcNow.AddHours(6)
        });

        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        var authController = new AuthController(logger.Object,oidcProvider.Object,authOptionsMock.Object,tokenStorage.Object,cookies.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var opaqueToken = "TestOpaqueToken";

        authController.HttpContext.Request.Headers.Add("Cookie", $"Authorization={opaqueToken}");

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

        authController.HttpContext.Request.Headers.Add("Cookie", $"Authorization=");

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

        authController.HttpContext.Request.Headers.Add("Cookie", $"Authorization={opaqueToken}");

        var response = authController.ForwardAuth();

        Assert.Equal(401, ((StatusCodeResult)response).StatusCode);
    }
}
