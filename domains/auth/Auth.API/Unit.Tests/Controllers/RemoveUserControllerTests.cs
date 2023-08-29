using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class RemoveUserControllerTests
{
    private readonly IUserDescriptorMapper mapper = Mock.Of<IUserDescriptorMapper>();
    private readonly RemoveUserController controller = new();
    private readonly ILogger<RemoveUserController> logger = Mock.Of<ILogger<RemoveUserController>>();
    private readonly IUserService userService = Mock.Of<IUserService>();

    [Fact]
    public async Task RemoveUser_ShouldReturnsBadRequest_WhenSelfDeletion()
    {
        var userId = Guid.NewGuid();
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns(new UserDescriptor(null!) { Id = userId });

        var result = await controller.RemoveUser(userId, mapper, userService, logger);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnOk_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns(new UserDescriptor(null!) { Id = Guid.NewGuid() });
        Mock.Get(userService).Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User)null!);

        var result = await controller.RemoveUser(userId, mapper, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnsOk_WhenSuccessfulDeletion()
    {
        var userId = Guid.NewGuid();
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns(new UserDescriptor(null!) { Id = Guid.NewGuid() });
        Mock.Get(userService).Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(new User());

        var result = await controller.RemoveUser(userId, mapper, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveUser_ShouldThrowException_WhenMapperFails()
    {
        var userId = Guid.NewGuid();
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Throws(new NullReferenceException());

        await Assert.ThrowsAsync<NullReferenceException>(() => controller.RemoveUser(userId, mapper, userService, logger));
    }
}
