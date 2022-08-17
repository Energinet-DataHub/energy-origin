using API.Controllers;
using API.Models;
using API.Services.OidcProviders;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public class AuthControllerTest
{
    private readonly Mock<IOidcProviders> _mockSignaturGruppen = new();
    private readonly Mock<ILogger<AuthController>> _mockLogger = new();
    private AuthController _authController;

    public AuthControllerTest()
    {
        _authController = new AuthController(_mockLogger.Object, _mockSignaturGruppen.Object);
    }

    [Fact]
    public void can_invalidate_token()
    {
        var authState = new AuthState()
        {
            IdToken = "test"
        };

        var response = _authController.Invalidate(authState);
        _mockSignaturGruppen.Verify(mock => mock.Logout(authState.IdToken), Times.Once);

        Assert.True(response);
    }

    [Fact]
    public void return_false_when_no_token()
    {
        var authState = new AuthState();

        var response = _authController.Invalidate(authState);

        _mockSignaturGruppen.Verify(mock => mock.Logout(authState.Tin), Times.Never);
        Assert.False(response);
    }
}
