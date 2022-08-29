using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using API.Configuration;
using API.Controllers;
using API.Controllers.dto;
using API.Errors;
using API.Models;
using API.Services;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Categories;

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

    [Theory]
    [InlineData("error", true)]
    [InlineData(null, false)]
    public void OidcProviders_IsError(string error, bool expectedIsError)
    {
        var oidcServiceMock = new Mock<IOidcService>();
        var authOptionsMock = new Mock<IOptions<AuthOptions>>();

        var signaturGruppen = new SignaturGruppen(new Mock<ILogger<SignaturGruppen>>().Object, oidcServiceMock.Object, authOptionsMock.Object, new HttpClient());

        OidcCallbackParams oidcCallbackParams = new OidcCallbackParams() { Error = error };

        Assert.Equal(expectedIsError, signaturGruppen.isError(oidcCallbackParams));
    }

    [Theory]
    [InlineData("mitid_user_aborted", "bar?success=0&error_code=E1&error=User%20interrupted")]
    [InlineData("user_aborted", "bar?success=0&error_code=E1&error=User%20interrupted")]
    [InlineData("foo", "bar?success=0&error_code=E0&error=Unknown%20error%20from%20Identity%20Provider")]
    public void OidcProviders_OnOidcFlowFailed(string ErrorDescription, string expectedNextUrl)
    {
        var oidcServiceMock = new Mock<IOidcService>();
        var authOptionsMock = new Mock<IOptions<AuthOptions>>();

        var signaturGruppen = new SignaturGruppen(new Mock<ILogger<SignaturGruppen>>().Object, oidcServiceMock.Object, authOptionsMock.Object, new HttpClient());

        var state = new AuthState
        {
            FeUrl = "foo",
            ReturnUrl = "bar",
        };

        OidcCallbackParams oidcCallbackParams = new OidcCallbackParams() { ErrorDescription = ErrorDescription  };

        var res = signaturGruppen.OnOidcFlowFailed(state, oidcCallbackParams);

        Assert.Equal(expectedNextUrl, res.NextUrl);
    }

}
