using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class RoleControllerTests
{
    // FIXME: add tests for listing roles, tests for only manipulating the companys own users, just add all missing

    private readonly IUserDescriptorMapper mapper = Mock.Of<IUserDescriptorMapper>();
    private readonly RoleController roleController = new();
    private readonly ILogger<RoleController> logger = Mock.Of<ILogger<RoleController>>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly RoleOptions roleOptions;
    public RoleControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        roleOptions = configuration.GetSection(RoleOptions.Prefix).Get<RoleOptions>()!;
    }

    [Fact]
    public async Task AssignRole_ShouldThrowException_WhenUserDescriptorMappingFails()
    {
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns((UserDescriptor)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => roleController.AssignRole(RoleKey.Viewer, Guid.NewGuid(), roleOptions, userService, logger, mapper));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    [InlineData("___")]
    [InlineData("whatever")]
    [InlineData("role")]
    [InlineData("IDK")]
    public async Task AssignRole_ShouldThrowException_WhenRoleIsInvalid(string role)
    {
        var response = await roleController.AssignRole(role, Guid.NewGuid(), roleOptions, userService, logger, mapper);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task AssignRole_ShouldThrowException_WhenUserIsNull()
    {
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => roleController.AssignRole(RoleKey.Viewer, Guid.NewGuid(), roleOptions, userService, logger, mapper));
    }

    [Fact]
    public async Task AssignRole_ShouldReturnOk_WhenSuccess()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid()
            });

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        var dummyUser = new User();
        Mock.Get(userService).Setup(service => service.UpsertUserAsync(testUser)).ReturnsAsync(dummyUser);
        var result = await roleController.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger, mapper);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldRemoveRoleFromUser_WhenInvoked()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid()
            });
        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        roleController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.NameIdentifier, testUserId.ToString()) })) }
        };

        var result = await roleController.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger, mapper);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldThrowException_WhenUserIsNull()
    {
        var testUserId = Guid.NewGuid();
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync((User)null!);
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            roleController.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger, mapper));
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldThrowException_WhenUserRoleIsNull()
    {
        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        await Assert.ThrowsAsync<NullReferenceException>(() => roleController.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger, mapper));
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldThrowException_WhenUserDescriptorMappingFails()
    {
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns((UserDescriptor)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => roleController.RemoveRoleFromUser(RoleKey.Viewer, Guid.NewGuid(), userService, logger, mapper));
    }
}
