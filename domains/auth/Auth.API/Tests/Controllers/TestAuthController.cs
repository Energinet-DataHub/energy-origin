using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using API.Configuration;
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

[UnitTest]
public class TestAuthController
{
    private readonly Mock<IOidcProviders> mockSignaturGruppen = new();
    private readonly Mock<ILogger<AuthController>> logger = new();
    private readonly Mock<ITokenStorage> tokenStorage = new();
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ICryptographyService> cryptographyService = new();
    private readonly InvalidateAuthStateValidator validator = new();

    private AuthController authController;

    public TestAuthController()
    {
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        authController = new AuthController(
            logger.Object,
            mockSignaturGruppen.Object,
            authOptionsMock.Object,
            tokenStorage.Object,
            cryptographyService.Object,
            validator
        )
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task CanInvalidateToken()
    {
        var authState = new AuthState()
        {
            IdToken = "test"
        };

        var authStateAsString = JsonSerializer.Serialize(authState);

        cryptographyService
            .Setup(x => x.Decrypt<AuthState>(It.IsAny<string>()))
            .Returns(authState);

        var response = await authController.Invalidate(authStateAsString);

        mockSignaturGruppen.Verify(mock => mock.Logout(authState.IdToken), Times.Once);
        Assert.IsType<OkResult>(response);
    }

    [Fact]
    public async Task ReturnBadRequestWhenNoState()
    {
        var response = await authController.Invalidate("");

        Assert.IsType<BadRequestResult>(response);
    }

    [Fact]
    public async Task ReturnBadRequestWhenNoToken()
    {
        var authState = new AuthState();
        var authStateAsString = JsonSerializer.Serialize(authState);
        cryptographyService
            .Setup(x => x.Decrypt<AuthState>(It.IsAny<string>()))
            .Returns(authState);

        var response = await authController.Invalidate(authStateAsString) as ObjectResult;
        Assert.IsType<ValidationProblemDetails>(response?.Value);
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

        authController.HttpContext.Response.Cookies.Append("Authorization", opaqueToken, notExpiredCookie);
        authController.HttpContext.Request.Headers.Add("Authorization", testToken);

        authController.Logout();

        Assert.Equal(
            expectedExpiredCookie,
            authController.HttpContext.Response.GetTypedHeaders().SetCookie.Single().ToString()
        );
    }
}
