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

    [Theory]
    [InlineData("Bearer foo")]
    [InlineData(null)]
    public void LogoutDeleteCookieSuccess(string? testToken)
    {
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

        logoutController.HttpContext.Response.Cookies.Append("Authorization", opaqueToken, notExpiredCookie);
        logoutController.HttpContext.Request.Headers.Add("Authorization", testToken);

        logoutController.Logout();

        Assert.Equal(
            expectedExpiredCookie,
            logoutController.HttpContext.Response.GetTypedHeaders().SetCookie.Single().ToString()
        );
    }
}
