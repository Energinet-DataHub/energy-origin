using API.Configuration;
using API.Controllers;
using API.Controllers.dto;
using API.Errors;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Xunit;
using Xunit.Categories;

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

        var authController = new AuthController(logger.Object, oidcProvider.Object, authOptionsMock.Object, tokenStorage.Object)
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
