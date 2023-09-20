using System.Net;
using API.Models.Entities;
using API.Values;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests.Controllers;

public class RemoveUserControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    private readonly User user;

    public RemoveUserControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
        user = new() { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString(), Company = new() { Tin = Guid.NewGuid().ToString(), Name = Guid.NewGuid().ToString() } };
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnUnauthorized_WhenInvokedWithoutAuthorization()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.DeleteAsync($"user/remove/{Guid.NewGuid()}");

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnForbidden_WhenNonAdminUser()
    {
        var client = factory.CreateAuthenticatedClient(user);
        var userId = (await factory.AddUserToDatabaseAsync()).Id;

        var response = await client.DeleteAsync($"user/remove/{userId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnOk_WhenUserIsRemoved()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.UserAdmin);
        var userId = (await factory.AddUserToDatabaseAsync()).Id;

        var response = await client.DeleteAsync($"user/remove/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var deletedUser = await factory.DataContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnOk_WhenUserDoesNotExist()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.UserAdmin);

        var response = await client.DeleteAsync($"user/remove/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnBadRequest_WhenUserTriesToDeleteThemselves()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.UserAdmin);
        _ = await factory.AddUserToDatabaseAsync(user);

        var response = await client.DeleteAsync($"user/remove/{user.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}
