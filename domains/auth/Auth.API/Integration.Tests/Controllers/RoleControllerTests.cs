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
    private readonly User user = new()
    {
        Id = Guid.NewGuid(),
        Name = Guid.NewGuid().ToString()
    };
    private readonly User adminUser = new()
    {
        Id = Guid.NewGuid(),
        Name = Guid.NewGuid().ToString(),
        UserRoles = new List<UserRole> { new() { Role = RoleKey.Admin } }
    };

    public RoleControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Assign_ReturnsOk_WhenRoleIsAssigned()
    {
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);

        var role = RoleKey.Viewer;
        var response = await client.PutAsync($"role/{role}/assign/{user.Id}", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserRoles).FirstOrDefault(x => x.Id == user.Id)!;

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(role, dbUser.UserRoles.First()?.Role);
    }

    [Fact]
    public async Task Assign_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);

        var response = await client.PutAsync($"role/{RoleKey.Viewer}/assign/{Guid.NewGuid()}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_ShouldReturnBadRequest_WhenRoleDoesNotExist()
    {
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);

        var response = await client.PutAsync($"role/notExistentRoleKey/assign/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Remove_ShouldReturnOk_WhenRoleExists()
    {
        var role = RoleKey.Viewer;
        var dataContext = factory.DataContext;
        var userWithRole = new User
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
        await dataContext.AddAsync(userWithRole);
        await dataContext.SaveChangesAsync();

        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);
        var response = await client.PutAsync($"role/{role}/remove/{userWithRole.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(factory.DataContext.Users.FirstOrDefault(x => x.Id == userWithRole.Id)?.UserRoles.Any(x => x.Role == role) ?? true);
    }

    [Fact]
    public async Task Remove_ShouldReturnOk_WhenRoleDoesNotExist()
    {
        var role = RoleKey.Viewer;
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(this.user, role: RoleKey.Admin);

        var response = await client.PutAsync($"role/{role}/remove/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Remove_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);

        var response = await client.PutAsync($"role/{RoleKey.Viewer}/remove/{Guid.NewGuid()}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_ShouldReturnBadRequest_WhenUserTriesToRemoveAdminFromThemselves()
    {
        var user = await factory.AddUserToDatabaseAsync(adminUser);
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);
        var response = await client.PutAsync($"role/{RoleKey.Admin}/remove/{user.Id}", null);

        Assert.NotNull(response);
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

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("remove")]
    public async Task RoleCalls_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull(string action)
    {
        var role = RoleKey.Viewer;
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin, config: builder =>
        {
            var mapper = Mock.Of<IUserDescriptorMapper>();
            Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services => services.AddScoped(_ => mapper));
        });

        var response = await client.PutAsync($"role/{role}/{action}/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
