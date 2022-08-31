using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Configuration;
using API.Models;
using API.Repository;
using API.Services;
using API.Services.OidcProviders;
using API.TokenStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace API.Controllers;

public class TestLogoutController
{
    private readonly Mock<IOidcService> mockSignaturGruppen = new();
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ITokenStorage> tokenStorage = new();

    private readonly LogoutController logoutController;

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
    public async Task LogoutDeleteCookieReturnSuccess(string? testCookie)
    {
        var expectedExpiredCookie = "Authorization=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";

        logoutController.HttpContext.Request.Headers.Add("Cookie", testCookie);

        await logoutController.Logout();

        Assert.Equal(expectedExpiredCookie, logoutController.HttpContext.Response.GetTypedHeaders().SetCookie.Single().ToString());
    }


}
