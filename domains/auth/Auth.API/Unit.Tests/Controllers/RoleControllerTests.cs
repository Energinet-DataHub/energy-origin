using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Values;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class RoleControllerTests
{
    private readonly RoleController controller = new();
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
    public void List_ShouldReturnExpected_WhenInvoked()
    {
        var response = controller.List(roleOptions);

        Assert.NotNull(response);
        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task Assign_ShouldThrowException_WhenUserDescriptorMappingFails()
    {
        controller.SetUser();
        // Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns((UserDescriptor)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => controller.AssignRole(RoleKey.Viewer, Guid.NewGuid(), roleOptions, userService, logger));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    [InlineData("___")]
    [InlineData("whatever")]
    [InlineData("role")]
    [InlineData("IDK")]
    public async Task Assign_ShouldThrowException_WhenRoleIsInvalid(string role)
    {
        var response = await controller.AssignRole(role, Guid.NewGuid(), roleOptions, userService, logger);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task Assign_ShouldThrowException_WhenUserIsNull()
    {
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => controller.AssignRole(RoleKey.Viewer, Guid.NewGuid(), roleOptions, userService, logger));
    }

    [Fact]
    public async Task Assign_ShouldReturnOk_WhenInvoked()
    {
        controller.SetUser();
        // Mock.Get(mapper)
        //     .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
        //     .Returns(new UserDescriptor(null!)
        //     {
        //         Id = Guid.NewGuid()
        //     });

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        var dummyUser = new User();
        Mock.Get(userService).Setup(service => service.UpsertUserAsync(testUser)).ReturnsAsync(dummyUser);

        var result = await controller.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Assign_ShouldReturnBadRequest_WhenInvokedOnUserFromAnotherCompany()
    {
        controller.SetUser();
        // Mock.Get(mapper)
        //     .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
        //     .Returns(new UserDescriptor(null!)
        //     {
        //         Id = Guid.NewGuid(),
        //         Tin = Guid.NewGuid().ToString(),
        //     });

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId, Company = new() { Tin = Guid.NewGuid().ToString() } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        var dummyUser = new User();
        Mock.Get(userService).Setup(service => service.UpsertUserAsync(testUser)).ReturnsAsync(dummyUser);

        var result = await controller.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Assign_ShouldReturnBadRequest_WhenInvokedByPrivateUser()
    {
        controller.SetUser();
        // Mock.Get(mapper)
        //     .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
        //     .Returns(new UserDescriptor(null!)
        //     {
        //         Id = Guid.NewGuid()
        //     });

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId, Company = new() { Tin = Guid.NewGuid().ToString() } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        var dummyUser = new User();
        Mock.Get(userService).Setup(service => service.UpsertUserAsync(testUser)).ReturnsAsync(dummyUser);

        var result = await controller.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldRemove_WhenInvoked()
    {
        controller.SetUser();
        // Mock.Get(mapper)
        //     .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
        //     .Returns(new UserDescriptor(null!)
        //     {
        //         Id = Guid.NewGuid()
        //     });
        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.NameIdentifier, testUserId.ToString()) })) }
        };

        var result = await controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldRemove_WhenInvokedOnUserFromAnotherCompany()
    {
        controller.SetUser();
        // Mock.Get(mapper)
        //     .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
        //     .Returns(new UserDescriptor(null!)
        //     {
        //         Id = Guid.NewGuid(),
        //         Tin = Guid.NewGuid().ToString(),
        //     });
        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole }, Company = new() { Tin = Guid.NewGuid().ToString() } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.NameIdentifier, testUserId.ToString()) })) }
        };

        var result = await controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldRemove_WhenInvokedByPrivateUser()
    {
        controller.SetUser();

        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole }, Company = new() { Tin = Guid.NewGuid().ToString() } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        var result = await controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldThrowException_WhenUserIsNull()
    {
        var testUserId = Guid.NewGuid();

        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync((User)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger));
    }

    [Fact]
    public async Task Remove_ShouldThrowException_WhenUserRoleIsNull()
    {
        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId };

        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        await Assert.ThrowsAsync<NullReferenceException>(() => controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger));
    }

    [Fact]
    public async Task Remove_ShouldThrowException_WhenUserDescriptorMappingFails()
    {
        controller.SetUser();
        // Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns((UserDescriptor)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => controller.RemoveRoleFromUser(RoleKey.Viewer, Guid.NewGuid(), userService, logger));
    }
}
