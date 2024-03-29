using System.Net;
using API.Models.Entities;
using API.Options;
using API.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Controllers;

public class RoleControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    private readonly User user;
    private readonly User adminUser;

    public RoleControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
        var tin = Guid.NewGuid().ToString();
        adminUser = new()
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            UserRoles = new List<UserRole> { new() { Role = RoleKey.RoleAdmin } },
            Company = new() { Id = Guid.NewGuid(), Tin = tin, Name = Guid.NewGuid().ToString() }
        };
        user = new()
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Company = new() { Id = Guid.NewGuid(), Tin = tin, Name = Guid.NewGuid().ToString() }
        };
    }

    [Fact]
    public async Task List_ShouldReturnUnauthorized_WhenInvokedWithoutAuthorization()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.GetAsync($"role/all");

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Assign_ShouldReturnUnauthorized_WhenInvokedWithoutAuthorization()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.PutAsync($"role/{RoleKey.Viewer}/assign/{Guid.NewGuid()}", null);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Remove_ShouldReturnUnauthorized_WhenInvokedWithoutAuthorization()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.PutAsync($"role/{RoleKey.Viewer}/remove/{Guid.NewGuid()}", null);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk_WhenInvoked()
    {
        var user = await factory.AddUserToDatabaseAsync(this.user);
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin);

        var response = await client.GetAsync($"role/all");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Assign_ReturnsOk_WhenRoleIsAssigned()
    {
        var user = await factory.AddUserToDatabaseAsync(this.user);
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin);

        var role = RoleKey.Viewer;
        var response = await client.PutAsync($"role/{role}/assign/{user.Id}", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserRoles).SingleOrDefault(x => x.Id == user.Id)!;

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(role, dbUser.UserRoles.First()?.Role);
    }

    [Fact]
    public async Task Assign_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin);

        var response = await client.PutAsync($"role/{RoleKey.Viewer}/assign/{Guid.NewGuid()}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_ShouldReturnBadRequest_WhenRoleDoesNotExist()
    {
        var role = "any";
        var options = new RoleOptions()
        {
            RoleConfigurations = new() { new() { Key = role, Name = role } }
        };
        var user = await factory.AddUserToDatabaseAsync(this.user);
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        var response = await client.PutAsync($"role/notExistentRoleKey/assign/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assign_ShouldReturnBadRequest_WhenRoleIsTransient()
    {
        var role = "transient";
        var options = new RoleOptions()
        {
            RoleConfigurations = new() { new() { Key = role, Name = role, IsTransient = true } }
        };
        var user = await factory.AddUserToDatabaseAsync(this.user);
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        var response = await client.PutAsync($"role/{role}/assign/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Remove_ShouldReturnOk_WhenRoleIsAssigned()
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
            Company = new() { Id = Guid.NewGuid(), Tin = user.Company!.Tin, Name = "" },
            AllowCprLookup = false,
            Name = "TestUser"
        };
        await dataContext.AddAsync(userWithRole);
        await dataContext.SaveChangesAsync();

        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin);
        var response = await client.PutAsync($"role/{role}/remove/{userWithRole.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(factory.DataContext.Users.SingleOrDefault(x => x.Id == userWithRole.Id)?.UserRoles.Any(x => x.Role == role) ?? true);
    }

    [Fact]
    public async Task Remove_ShouldReturnOk_WhenRoleIsNotAssigned()
    {
        var role = RoleKey.Viewer;
        var user = await factory.AddUserToDatabaseAsync(this.user);
        var client = factory.CreateAuthenticatedClient(adminUser, role: RoleKey.RoleAdmin);

        var response = await client.PutAsync($"role/{role}/remove/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Remove_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin);

        var response = await client.PutAsync($"role/{RoleKey.Viewer}/remove/{Guid.NewGuid()}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_ShouldReturnBadRequest_WhenUserTriesToRemoveAdminFromThemselves()
    {
        var user = await factory.AddUserToDatabaseAsync(adminUser);
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin);
        var response = await client.PutAsync($"role/{RoleKey.RoleAdmin}/remove/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("remove")]
    public async Task RoleCalls_ShouldReturnForbidden_WhenNonAdminUser(string action)
    {
        var role = RoleKey.Viewer;
        var user = await factory.AddUserToDatabaseAsync(this.user);
        var client = factory.CreateAuthenticatedClient(user);
        var response = await client.PutAsync($"role/{role}/{action}/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("remove")]
    public async Task RoleCalls_ShouldReturnForbidden_WhenAdminUserButPrivate(string action)
    {
        var role = RoleKey.Viewer;
        var user = await factory.AddUserToDatabaseAsync(new() { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() });
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.RoleAdmin);
        var response = await client.PutAsync($"role/{role}/{action}/{user.Id}", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsersByTin_ShouldReturnOk_WhenTinIsValid()
    {
        var adminUser = await factory.AddUserToDatabaseAsync(this.adminUser);
        var client = factory.CreateAuthenticatedClient(adminUser, role: RoleKey.RoleAdmin);
        var response = await client.GetAsync($"role/users");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
