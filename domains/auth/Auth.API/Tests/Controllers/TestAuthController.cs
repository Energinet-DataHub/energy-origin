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
    private readonly Mock<IOidcProviders> _mockSignaturGruppen = new();
    private readonly Mock<ILogger<AuthController>> logger = new();
    private readonly Mock<ITokenStorage> tokenStorage = new();
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ICryptographyService> _cryptographyService = new();
    private readonly InvalidateAuthStateValidator _validator = new();

    private AuthController _authController;

    public TestAuthController()
    {
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        _authController = new AuthController(logger.Object, _mockSignaturGruppen.Object, authOptionsMock.Object,
            tokenStorage.Object, _cryptographyService.Object, _validator)
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

        _cryptographyService
            .Setup(x => x.Decrypt<AuthState>(It.IsAny<string>()))
            .Returns(authState);

        var response = await _authController.Invalidate(authStateAsString);

        _mockSignaturGruppen.Verify(mock => mock.Logout(authState.IdToken), Times.Once);
        Assert.IsType<OkResult>(response);
    }

    [Fact]
    public async Task ReturnBadRequestWhenNoState()
    {
        var response = await _authController.Invalidate("");

        Assert.IsType<BadRequestObjectResult>(response);
        var result = response as BadRequestObjectResult;
        Assert.Equal("Cannot decrypt " + nameof(AuthState), result?.Value);
    }

    [Fact]
    public async Task ReturnBadRequestWhenNoToken()
    {
        var authState = new AuthState();
        var authStateAsString = JsonSerializer.Serialize(authState);
        _cryptographyService
            .Setup(x => x.Decrypt<AuthState>(It.IsAny<string>()))
            .Returns(authState);

        var response = await _authController.Invalidate(authStateAsString) as ObjectResult;
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


        _authController.HttpContext.Response.Cookies.Append("Authorization", opaqueToken, notExpiredCookie);
        _authController.HttpContext.Request.Headers.Add("Authorization", testToken);

        _authController.Logout();

        //Assert
        Assert.Equal(
            expectedExpiredCookie,
            _authController.HttpContext.Response.GetTypedHeaders().SetCookie.Single().ToString()
        );
    }
}
