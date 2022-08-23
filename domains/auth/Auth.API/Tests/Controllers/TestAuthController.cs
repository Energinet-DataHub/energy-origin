﻿using System;
using System.Linq;
using System.Text.Json;
using API.Configuration;
using API.Helpers;
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
    private readonly Mock<IOidcService> _mockSignaturGruppen = new();
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ICryptography> _cryptographyService = new();
    private readonly InvalidateAuthStateValidator _validator = new();

    private InvalidateController _invalidateController;

    public TestAuthController()
    {
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions
        {
            CookieName = "Authorization",
        });

        _invalidateController = new InvalidateController(_mockSignaturGruppen.Object, _cryptographyService.Object, _validator)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public void CanInvalidateToken()
    {
        var authState = new AuthState()
        {
            IdToken = "test"
        };

        var authStateAsString = JsonSerializer.Serialize(authState);

        _cryptographyService
            .Setup(x => x.Decrypt(authStateAsString))
            .Returns(authStateAsString);

        var response = _invalidateController.Invalidate(authStateAsString);

        _mockSignaturGruppen.Verify(mock => mock.Logout(authState.IdToken), Times.Once);
        Assert.IsType<OkResult>(response);
    }

    [Fact]
    public void ReturnBadRequestWhenNoState()
    {
        var response = _invalidateController.Invalidate("");

        Assert.IsType<BadRequestObjectResult>(response);
        var result = response as BadRequestObjectResult;
        Assert.Equal("Cannot decrypt " + nameof(AuthState), result?.Value);
    }

    [Fact]
    public void ReturnBadRequestWhenNoToken()
    {
        var authState = new AuthState();
        var authStateAsString = JsonSerializer.Serialize(authState);
        _cryptographyService
            .Setup(x => x.Decrypt(authStateAsString))
            .Returns(authStateAsString);

        var response = _invalidateController.Invalidate(authStateAsString);

        Assert.IsType<BadRequestObjectResult>(response);
        var result = response as BadRequestObjectResult;
        Assert.Equal(nameof(AuthState.IdToken) + " must not be null", result?.Value);
    }

    // [Theory]
    // [InlineData("Bearer foo")]
    // [InlineData(null)]
    // public void LogoutDeleteCookieSuccess(string? testToken)
    // {
    //     var opaqueToken = "TestOpaqueToken";
    //     var expectedExpiredCookie = "Authorization=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";
    //
    //     var notExpiredCookie = new CookieOptions
    //     {
    //         Path = "/",
    //         Domain = "energioprindelse.dk",
    //         HttpOnly = true,
    //         SameSite = SameSiteMode.Strict,
    //         Secure = true,
    //         Expires = DateTime.UtcNow.AddHours(6),
    //     };
    //
    //
    //     _invalidateController.HttpContext.Response.Cookies.Append("Authorization", opaqueToken, notExpiredCookie);
    //     _invalidateController.HttpContext.Request.Headers.Add("Authorization", testToken);
    //
    //     _invalidateController.Logout();
    //
    //     //Assert
    //     Assert.Equal(expectedExpiredCookie,
    //         _invalidateController.HttpContext.Response.GetTypedHeaders().SetCookie.Single().ToString());
    // }
}
