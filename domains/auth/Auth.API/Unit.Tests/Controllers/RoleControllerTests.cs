using API.Controllers;
using API.Models.Entities;
using API.Models.Response;
using API.Options;
using API.Services.Interfaces;
using API.Values;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Controllers;

public class RoleControllerTests
{
    private readonly RoleController controller = new();
    private readonly ILogger<RoleController> logger = Substitute.For<ILogger<RoleController>>();
    private readonly IUserService userService = Substitute.For<IUserService>();
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
    public async Task Assign_ShouldThrowException_WhenPrincipalIsNull() => await Assert.ThrowsAsync<PropertyMissingException>(() => controller.AssignRole(RoleKey.Viewer, Guid.NewGuid(), roleOptions, userService, logger));

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
    public async Task Assign_ShouldReturnOk_WhenInvoked()
    {
        controller.PrepareUser();

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId };
        userService.GetUserByIdAsync(testUserId).Returns(testUser);
        var dummyUser = new User();
        userService.UpsertUserAsync(testUser).Returns(dummyUser);

        var result = await controller.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Assign_ShouldReturnBadRequest_WhenInvokedOnUserFromAnotherCompany()
    {
        controller.PrepareUser();

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId, Company = new() { Tin = Guid.NewGuid().ToString() } };
        userService.GetUserByIdAsync(testUserId).Returns(testUser);
        var dummyUser = new User();
        userService.UpsertUserAsync(testUser).Returns(dummyUser);

        var result = await controller.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Assign_ShouldReturnBadRequest_WhenInvokedByPrivateUser()
    {
        controller.PrepareUser();

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId, Company = new() { Tin = Guid.NewGuid().ToString() } };
        userService.GetUserByIdAsync(testUserId).Returns(testUser);
        var dummyUser = new User();
        userService.UpsertUserAsync(testUser).Returns(dummyUser);

        var result = await controller.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldRemove_WhenInvoked()
    {
        controller.PrepareUser();

        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole } };
        userService.GetUserByIdAsync(testUserId).Returns(testUser);

        var result = await controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldNotRemove_WhenInvokedOnUserFromAnotherCompany()
    {
        controller.PrepareUser(organization: new()
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Tin = Guid.NewGuid().ToString()
        });

        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole }, Company = new() { Tin = Guid.NewGuid().ToString() } };
        userService.GetUserByIdAsync(testUserId).Returns(testUser);

        var result = await controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldRemove_WhenInvokedByPrivateUser()
    {
        controller.PrepareUser();

        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole }, Company = new() { Tin = Guid.NewGuid().ToString() } };
        userService.GetUserByIdAsync(testUserId).Returns(testUser);

        var result = await controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldThrowException_WhenUserIsNull()
    {
        var testUserId = Guid.NewGuid();

        userService.GetUserByIdAsync(testUserId).Returns((User)null!);

        await Assert.ThrowsAsync<PropertyMissingException>(() => controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger));
    }

    [Fact]
    public async Task Remove_ShouldReturnOk_WhenUserRoleIsNull()
    {
        controller.PrepareUser();

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId };

        userService.GetUserByIdAsync(testUserId).Returns(testUser);

        var result = await controller.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger);

        Assert.IsType<OkResult>(result);
    }

    public static IEnumerable<object[]> UserData =>
    new List<object[]>
    {
        new object[]
        {
            new List<User>
            {
            }
        },
        new object[]
        {
            new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "test",
                    UserRoles = new List<UserRole>
                    {
                        new UserRole { Role = RoleKey.UserAdmin }
                    }
                }
            }
        },
        new object[]
        {
            new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "test",
                    UserRoles = new List<UserRole>
                    {
                        new UserRole { Role = RoleKey.OrganizationAdmin }
                    }
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "test2",
                    UserRoles = new List<UserRole>
                    {
                        new UserRole { Role = RoleKey.RoleAdmin },
                        new UserRole { Role = RoleKey.Viewer }
                    }
                }
            }
        }
    };

    [Theory]
    [MemberData(nameof(UserData))]
    public async Task GetUsersByTin_ShouldReturnOkAndTransformToResponseObject_WhenValidTinIsProvided(List<User> users)
    {
        Mock.Get(userService)
            .Setup(service => service.GetUsersByTinAsync(It.IsAny<string>()))
            .ReturnsAsync(users);

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid(),
                Tin = Guid.NewGuid().ToString(),
            });

        var testUserId = Guid.NewGuid();
        roleController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.NameIdentifier, testUserId.ToString()) })) }
        };
        var result = await roleController.GetUsersByTin(userService, mapper, roleOptions, logger);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<List<UserRolesResponse>>(okResult.Value);

        Assert.Equal(users.Count, returnValue.Count);
        for (int i = 0; i < returnValue.Count; i++)
        {
            UserRolesResponse response = returnValue[i];
            User user = users[i];

            Assert.Equal(user.Name, response.Name);
            Assert.Equal(user.UserRoles.Count, response.Roles.Count);

            for (int j = 0; j < user.UserRoles.Count; j++)
            {
                Assert.True(response.Roles.ContainsKey(user.UserRoles[j].Role));
            }
        }
    }

    [Fact]
    public async Task GetUsersByTin_ShouldFilterOutInvalidRoles_WhenRolesAreInvalid()
    {
        Mock.Get(userService)
            .Setup(service => service.GetUsersByTinAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<User> {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "test",
                    UserRoles = new List<UserRole>
                    {
                        new() { Role = "non-existent-role" },
                        new() { Role = RoleKey.Viewer }
                    }
                }
            });

        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid(),
                Tin = Guid.NewGuid().ToString(),
            });

        var testUserId = Guid.NewGuid();
        roleController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.NameIdentifier, testUserId.ToString()) })) }
        };
        var result = await roleController.GetUsersByTin(userService, mapper, roleOptions, logger);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<List<UserRolesResponse>>(okResult.Value);

        Assert.Single(returnValue);

        var response = returnValue.Single();

        Assert.Single(response.Roles);
        Assert.True(response.Roles.Single().Key == RoleKey.Viewer);
    }
}
