using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Models.Requests;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class RoleControllerTests
{
    private readonly IUserDescriptorMapper mapper = Mock.Of<IUserDescriptorMapper>();
    private readonly RoleController roleController = new();
    private readonly ILogger<RoleController> logger = Mock.Of<ILogger<RoleController>>();
    private readonly IUserService userService = Mock.Of<IUserService>();
    private readonly IRoleService roleService = Mock.Of<IRoleService>();

    [Fact]
    public async Task AssignRole_ShouldThrowException_WhenUserDescriptorMappingFails()
    {
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns((UserDescriptor)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            roleController.AssignRole(new RoleRequest { UserId = Guid.NewGuid(), RoleKey = "testRole" }, roleService, userService, logger, mapper));
    }

    [Fact]
    public async Task AssignRole_ShouldThrowException_WhenRoleIsNull()
    {
        Mock.Get(roleService).Setup(service => service.GetRollByKeyAsync(It.IsAny<string>())).ReturnsAsync((Role)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            roleController.AssignRole(new RoleRequest { UserId = Guid.NewGuid(), RoleKey = "testRole" }, roleService, userService, logger, mapper));
    }

    [Fact]
    public async Task AssignRole_ShouldThrowException_WhenUserIsNull()
    {
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            roleController.AssignRole(new RoleRequest { UserId = Guid.NewGuid(), RoleKey = "testRole" }, roleService, userService, logger, mapper));
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
        var testRole = new Role { Id = Guid.NewGuid(), Key = "testRole" };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        Mock.Get(roleService).Setup(service => service.GetRollByKeyAsync("testRole")).ReturnsAsync(testRole);
        var dummyUser = new User();
        Mock.Get(userService).Setup(service => service.UpsertUserAsync(testUser)).ReturnsAsync(dummyUser);
        var result = await roleController.AssignRole(new RoleRequest { UserId = testUserId, RoleKey = "testRole" }, roleService, userService, logger, mapper);

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
        var roleKey = "roleKey";
        var role = new Role { Key = roleKey, Id = Guid.NewGuid() };
        var userRole = new UserRole { UserId = testUserId, Role = role, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        roleController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.NameIdentifier, testUserId.ToString()) })) }
        };

        var result = await roleController.RemoveRoleFromUser(new RoleRequest { UserId = testUserId, RoleKey = roleKey }, userService, logger, mapper);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldThrowException_WhenUserIsNull()
    {
        var testUserId = Guid.NewGuid();
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync((User)null!);
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            roleController.RemoveRoleFromUser(new RoleRequest { UserId = testUserId }, userService, logger, mapper));
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldThrowException_WhenUserRoleIsNull()
    {
        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            roleController.RemoveRoleFromUser(new RoleRequest { UserId = testUserId, RoleKey = "testRole" }, userService, logger, mapper));
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldThrowException_WhenUserDescriptorMappingFails()
    {
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns((UserDescriptor)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            roleController.RemoveRoleFromUser(new RoleRequest { UserId = Guid.NewGuid(), RoleKey = "testRole" }, userService, logger, mapper));
    }
}
