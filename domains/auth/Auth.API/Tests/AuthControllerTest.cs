using API.Controllers;
using API.Models;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Mvc;
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

        var response =  _authController.Invalidate(authState);
        _mockSignaturGruppen.Verify(mock => mock.Logout(authState.IdToken), Times.Once);
        var result = response as OkResult;
        Assert.NotNull(result);
    }

    [Fact]
    public void return_badrequest_when_no_token()
    {
        var authState = new AuthState();

        var response = _authController.Invalidate(authState);

        _mockSignaturGruppen.Verify(mock => mock.Logout(authState.IdToken), Times.Never);
        Assert.NotNull(response as BadRequestResult);
    }
}
