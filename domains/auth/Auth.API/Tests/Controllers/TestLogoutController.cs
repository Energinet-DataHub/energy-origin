using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using API.Configuration;
using API.Controllers.dto;
using API.Models;
using API.Repository;
using API.Services;
using API.Services.OidcProviders;
using API.Utilities;
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
    private readonly Mock<ICryptographyFactory> cryptographyFactory = new();
    private readonly Mock<IJwkService> jwkService = new();

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

    public static IEnumerable<object?[]> Cookie => new[]
    {
        new object?[] { $"Authorization=TestOpaqueToken;Path=/;Domain = energioprindelse.dk;HttpOnly = true; SameSite = SameSiteMode.Strict,Secure = true;Expires = {DateTime.UtcNow.AddHours(6)}" },
        new object?[] { null },
    };

    [Theory, MemberData(nameof(Cookie))]
    public async Task LogoutDeleteCookieReturnSuccess(string? testCookie)
    {
        var expectedExpiredCookie = "Authorization=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";

        logoutController.HttpContext.Request.Headers.Add("Cookie", testCookie);

        await logoutController.Logout();

        Assert.Equal(expectedExpiredCookie, logoutController.HttpContext.Response.GetTypedHeaders().SetCookie.Single().ToString());
    }

    [Theory]
    [InlineData("error", true)]
    [InlineData(null, false)]
    public void OidcProviders_IsError(string error, bool expectedIsError)
    {

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();

        var signaturGruppen = new SignaturGruppen(new Mock<ILogger<SignaturGruppen>>().Object, authOptionsMock.Object, new HttpClient(), cryptographyFactory.Object.StateCryptography(), jwkService.Object);

        var oidcCallbackParams = new OidcCallbackParams() { Error = error };

        Assert.Equal(expectedIsError, signaturGruppen.isError(oidcCallbackParams));
    }

    [Theory]
    [InlineData("mitid_user_aborted", "bar?success=0&error_code=E1&error=User%20interrupted")]
    [InlineData("user_aborted", "bar?success=0&error_code=E1&error=User%20interrupted")]
    [InlineData("foo", "bar?success=0&error_code=E0&error=Unknown%20error%20from%20Identity%20Provider")]
    public void OidcProviders_OnOidcFlowFailed(string ErrorDescription, string expectedNextUrl)
    {
        var authOptionsMock = new Mock<IOptions<AuthOptions>>();


        var signaturGruppen = new SignaturGruppen(new Mock<ILogger<SignaturGruppen>>().Object, authOptionsMock.Object, new HttpClient(), cryptographyFactory.Object.StateCryptography(), jwkService.Object);

        var state = new AuthState
        {
            FeUrl = "foo",
            ReturnUrl = "bar",
        };

        var oidcCallbackParams = new OidcCallbackParams() { ErrorDescription = ErrorDescription };

        var res = signaturGruppen.OnOidcFlowFailed(state, oidcCallbackParams);

        Assert.Equal(expectedNextUrl, res.NextUrl);
    }
}
