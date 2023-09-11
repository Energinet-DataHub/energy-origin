using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Services.Interfaces;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class RemoveUserControllerTests
{
    private readonly RemoveUserController controller = new();
    private readonly ILogger<RemoveUserController> logger = Mock.Of<ILogger<RemoveUserController>>();
    private readonly IUserService userService = Mock.Of<IUserService>();

    [Fact]
    public async Task RemoveUser_ShouldReturnsBadRequest_WhenSelfDeletion()
    {
        var userId = Guid.NewGuid();
        controller.PrepareUser(id: userId);

        var result = await controller.RemoveUser(userId, userService, logger);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnOk_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        controller.PrepareUser();
        Mock.Get(userService).Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User)null!);

        var result = await controller.RemoveUser(userId, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnsOk_WhenSuccessfulDeletion()
    {
        var userId = Guid.NewGuid();
        controller.PrepareUser();
        Mock.Get(userService).Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(new User());

        var result = await controller.RemoveUser(userId, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveUser_ShouldThrowException_WhenPrincipalIsNull() => await Assert.ThrowsAsync<PropertyMissingException>(() => controller.RemoveUser(Guid.NewGuid(), userService, logger));
}
