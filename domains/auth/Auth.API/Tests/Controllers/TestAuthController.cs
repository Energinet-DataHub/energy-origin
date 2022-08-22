using API.Controllers;
using API.Models;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace API.Controllers;

[UnitTest]
public class TestAuthController
{
    private readonly Mock<IOidcProviders> _mockSignaturGruppen = new();
    private AuthController _authController;

    public TestAuthController()
    {
        _authController = new AuthController(_mockSignaturGruppen.Object);
    }

    [Fact]
    public void CanInvalidateToken()
    {
        var authState = new AuthState()
        {
            IdToken = "test"
        };

        var response =  _authController.Invalidate(authState);
        _mockSignaturGruppen.Verify(mock => mock.Logout(authState.IdToken), Times.Once);
        Assert.IsType<OkResult>(response);
    }

    [Fact]
    public void ReturnBadRequestWhenNoToken()
    {
        var authState = new AuthState();

        var response = _authController.Invalidate(authState);

        _mockSignaturGruppen.Verify(mock => mock.Logout(authState.IdToken), Times.Never);

        Assert.IsType<BadRequestObjectResult>(response);
        var result = response as BadRequestObjectResult;
        Assert.Equal(nameof(authState.IdToken) +  " must not be null", result?.Value);

    }
}
