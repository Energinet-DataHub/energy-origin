using System.Net;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Controllers;

public class RemoveUserControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    private readonly User user;

    public RemoveUserControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
        user = new() { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnOk_WhenUserIsRemoved()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);
        var userId = (await factory.AddUserToDatabaseAsync()).Id;

        var response = await client.DeleteAsync($"user/remove/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var deletedUser = await factory.DataContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);

        var response = await client.DeleteAsync($"user/remove/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnBadRequest_WhenUserTriesToDeleteThemselves()
    {
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.Admin);
        _ = await factory.AddUserToDatabaseAsync(user);

        var response = await client.DeleteAsync($"user/remove/{user.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnInternalServerError_WhenUserDeletionFails()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var mockUserService = new Mock<IUserService>();
        mockUserService.Setup(service => service.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        mockUserService.Setup(service => service.RemoveUserAsync(user)).ReturnsAsync(false);

        var client = factory.CreateAuthenticatedClient(this.user, role: RoleKey.Admin, config: builder => builder.ConfigureTestServices(services => services.AddSingleton(mockUserService.Object)));

        var response = await client.DeleteAsync($"user/remove/{user.Id}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnForbidden_WhenNonAdminUser()
    {
        var client = factory.CreateAuthenticatedClient(user);
        var userId = (await factory.AddUserToDatabaseAsync()).Id;

        var response = await client.DeleteAsync($"user/remove/{userId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
