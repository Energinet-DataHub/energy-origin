using System.Security.Claims;
using API.Controllers;
using API.Models.Entities;
using API.Models.Response;
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
    public void List_ShouldReturnExpected_WhenInvoked()
    {
        var response = roleController.List(roleOptions);

        Assert.NotNull(response);
        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task Assign_ShouldThrowException_WhenUserDescriptorMappingFails()
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
    public async Task Assign_ShouldThrowException_WhenRoleIsInvalid(string role)
    {
        var response = await roleController.AssignRole(role, Guid.NewGuid(), roleOptions, userService, logger, mapper);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task Assign_ShouldThrowException_WhenUserIsNull()
    {
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => roleController.AssignRole(RoleKey.Viewer, Guid.NewGuid(), roleOptions, userService, logger, mapper));
    }

    [Fact]
    public async Task Assign_ShouldReturnOk_WhenInvoked()
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
    public async Task Assign_ShouldReturnBadRequest_WhenInvokedOnUserFromAnotherCompany()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid(),
                Tin = Guid.NewGuid().ToString(),
            });

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId, Company = new() { Tin = Guid.NewGuid().ToString() } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        var dummyUser = new User();
        Mock.Get(userService).Setup(service => service.UpsertUserAsync(testUser)).ReturnsAsync(dummyUser);

        var result = await roleController.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger, mapper);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Assign_ShouldReturnBadRequest_WhenInvokedByPrivateUser()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid()
            });

        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId, Company = new() { Tin = Guid.NewGuid().ToString() } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);
        var dummyUser = new User();
        Mock.Get(userService).Setup(service => service.UpsertUserAsync(testUser)).ReturnsAsync(dummyUser);

        var result = await roleController.AssignRole(RoleKey.Viewer, testUserId, roleOptions, userService, logger, mapper);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldRemove_WhenInvoked()
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
    public async Task Remove_ShouldRemove_WhenInvokedOnUserFromAnotherCompany()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid(),
                Tin = Guid.NewGuid().ToString(),
            });
        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole }, Company = new() { Tin = Guid.NewGuid().ToString() } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        roleController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.NameIdentifier, testUserId.ToString()) })) }
        };

        var result = await roleController.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger, mapper);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldRemove_WhenInvokedByPrivateUser()
    {
        Mock.Get(mapper)
            .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
            .Returns(new UserDescriptor(null!)
            {
                Id = Guid.NewGuid()
            });
        var testUserId = Guid.NewGuid();
        var userRole = new UserRole { UserId = testUserId, Role = RoleKey.Viewer, };
        var testUser = new User { Id = testUserId, UserRoles = new List<UserRole> { userRole }, Company = new() { Tin = Guid.NewGuid().ToString() } };
        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        roleController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new(ClaimTypes.NameIdentifier, testUserId.ToString()) })) }
        };

        var result = await roleController.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger, mapper);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Remove_ShouldThrowException_WhenUserIsNull()
    {
        var testUserId = Guid.NewGuid();

        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync((User)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => roleController.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger, mapper));
    }

    [Fact]
    public async Task Remove_ShouldThrowException_WhenUserRoleIsNull()
    {
        var testUserId = Guid.NewGuid();
        var testUser = new User { Id = testUserId };

        Mock.Get(userService).Setup(service => service.GetUserByIdAsync(testUserId)).ReturnsAsync(testUser);

        await Assert.ThrowsAsync<NullReferenceException>(() => roleController.RemoveRoleFromUser(RoleKey.Viewer, testUserId, userService, logger, mapper));
    }

    [Fact]
    public async Task Remove_ShouldThrowException_WhenUserDescriptorMappingFails()
    {
        Mock.Get(mapper).Setup(m => m.Map(It.IsAny<ClaimsPrincipal>())).Returns((UserDescriptor)null!);

        await Assert.ThrowsAsync<NullReferenceException>(() => roleController.RemoveRoleFromUser(RoleKey.Viewer, Guid.NewGuid(), userService, logger, mapper));
    }

    public static IEnumerable<object[]> UserData =>
    new List<object[]>
    {
        new object[]
        {
            new List<User> // List with 0 users
            {
            }
        },
        new object[]
        {
            new List<User> // List with 1 user
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
            new List<User> // List with 2 users
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
                        new UserRole {Role = RoleKey.Viewer}
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
}
