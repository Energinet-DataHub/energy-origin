using System.Net;
using System.Security.Claims;
using API.Models.Entities;
using API.Utilities.Interfaces;
using API.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Controllers;

public class RoleControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;

    public RoleControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Assign_ReturnsOk_WhenRoleIsAssigned()
    {
        var dbContext = factory.DataContext;
        var userId = Guid.NewGuid();
        await dbContext.Users.AddAsync(new User()
        {
            Id = userId, Name = "TestUser", AllowCprLookup = false
        });
        await dbContext.SaveChangesAsync();
        var client = factory.CreateAuthenticatedClient(SetupRoleAdminUser(), role: RoleKey.Admin);

        var role = RoleKey.Viewer;
        var response = await client.PutAsync($"role/{role}/assign/{userId}", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserRoles).FirstOrDefault(x => x.Id == userId)!;

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(role, dbUser.UserRoles.First()?.Role);
    }

    [Fact]
    public async Task Assign_ShouldThrowNullException_WhenUserDoesNotExist()
    {
        var client = factory.CreateAuthenticatedClient(SetupRoleAdminUser(), role: RoleKey.Admin);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await client.PutAsync($"role/some-role/assign/{Guid.NewGuid()}", null));
    }

    [Fact]
    public async Task Assign_ShouldThrowNullException_WhenRoleDoesNotExist()
    {
        var userId = Guid.NewGuid();
        await factory.AddUserToDatabaseAsync(new User
        {
            Id = userId, Name = "TestUser", AllowCprLookup = false
        });
        var client = factory.CreateAuthenticatedClient(SetupRoleAdminUser(), role: RoleKey.Admin);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await client.PutAsync($"role/notExistentRoleKey/assign/{userId}", null));
    }

    [Fact]
    public async Task Remove_ShouldRemoveRole_WhenRoleExists()
    {
        var role = RoleKey.Viewer;
        var dataContext = factory.DataContext;
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserRoles = new List<UserRole>
            {
                new()
                {
                    Role = role
                }
            },
            AllowCprLookup = false,
            Name = "TestUser"
        };
        await dataContext.AddAsync(user);
        await dataContext.SaveChangesAsync();

        var client = factory.CreateAuthenticatedClient(SetupRoleAdminUser(), role: RoleKey.Admin);
        var response = await client.PutAsync($"role/{role}/remove/{user.Id}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(factory.DataContext.Users.FirstOrDefault(x => x.Id == user.Id)?.UserRoles.Any(x => x.Role == role) ?? true);
    }

    [Fact]
    public async Task Remove_ShouldReturnOk_WhenRoleDoesNotExist()
    {
        var role = "notExistentRoleKey";
        var userId = Guid.NewGuid();
        var dataContext = factory.DataContext;
        var user = new User
        {
            Id = userId,
            AllowCprLookup = false,
            Name = "TestUser"
        };
        await dataContext.AddAsync(user);
        await dataContext.SaveChangesAsync();
        var client = factory.CreateAuthenticatedClient(SetupRoleAdminUser(), role: RoleKey.Admin);

        var response = await client.PutAsync($"role/{role}/remove/{userId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Remove_ShouldThrowException_WhenUserDoesNotExist()
    {
        var role = "TestRole";
        var userId = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(SetupRoleAdminUser(), role: RoleKey.Admin);

        await Assert.ThrowsAsync<NullReferenceException>(async () => await client.PutAsync($"role/{role}/remove/{userId}", null));
    }

    [Fact]
    public async Task Remove_ShouldReturnBadRequest_WhenUserTriesToRemoveAdminFromThemselves()
    {
        var user = await factory.AddUserToDatabaseAsync(SetupRoleAdminUser());
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);
        var response = await client.PutAsync($"role/{RoleKey.Admin}/remove/{user.Id}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Theory]
    [InlineData("assign")]
    [InlineData("remove")]
    public async Task RoleCalls_ShouldReturnForbidden_WhenNonAdminUser(string action)
    {
        var role = RoleKey.Viewer;
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user);
        var response = await client.PutAsync($"role/{role}/{action}/{user.Id}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("remove")]
    public async Task RoleCalls_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull(string action)
    {
        var role = RoleKey.Viewer;
        var user = SetupRoleAdminUser();
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin, config: builder =>
        {
            var mapper = Mock.Of<IUserDescriptorMapper>();
            Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services => services.AddScoped(_ => mapper));
        });

        await Assert.ThrowsAsync<NullReferenceException>(() => client.PutAsync($"role/{role}/{action}/{user.Id}", null));
    }

    private User SetupRoleAdminUser() => new()
    {
        Id = Guid.NewGuid(),
        UserRoles = new List<UserRole>
            {
                new()
                {
                    Role = RoleKey.Admin
                }
            },
        AllowCprLookup = true,
        Name = Guid.NewGuid().ToString()
    };
}
