using System.Net;
using API.Models.Entities;
using API.Services.Interfaces;
using API.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Controllers;

public class RemoveUserControllerTests: IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    private readonly User policyUser;

    public RemoveUserControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
        policyUser = SetupAuthPolicyUser();
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnNoContent_WhenUserIsRemoved()
    {
        var userToBeDeletedId = Guid.NewGuid();
        var user = new User { Id = userToBeDeletedId, Name = "Test User" };
        await factory.AddUserToDatabaseAsync(user);
        var client = factory.CreateAuthenticatedClient(policyUser);

        var response = await client.DeleteAsync($"user/remove/{userToBeDeletedId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var deletedUser = await factory.DataContext.Users.FirstOrDefaultAsync(x=>x.Id ==userToBeDeletedId);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var nonExistentUserId = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(policyUser);

        var response = await client.DeleteAsync($"user/remove/{nonExistentUserId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnBadRequest_WhenUserTriesToDeleteThemselves()
    {
        await factory.AddUserToDatabaseAsync(policyUser);
        var client = factory.CreateAuthenticatedClient(policyUser);

        var response = await client.DeleteAsync($"user/remove/{policyUser.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnInternalServerError_WhenUserDeletionFails()
    {
        var userToBeDeletedId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), Name = "Test User" };
        await factory.AddUserToDatabaseAsync(user);

        var mockUserService = new Mock<IUserService>();
        mockUserService.Setup(service => service.GetUserByIdAsync(userToBeDeletedId)).ReturnsAsync(user);
        mockUserService.Setup(service => service.RemoveUserAsync(user)).ReturnsAsync(false);
        var client = factory.CreateAuthenticatedClient(policyUser, config: builder => builder.ConfigureTestServices(services => services.AddSingleton(mockUserService.Object)));

        var response = await client.DeleteAsync($"user/remove/{userToBeDeletedId}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_ShouldReturnForbidden_WhenNonAdminUser()
    {
        var userToBeDeletedId = Guid.NewGuid();
        var user = new User { Id = userToBeDeletedId, Name = "Test User" };
        var client = factory.CreateAuthenticatedClient(user);

        var response = await client.DeleteAsync($"user/remove/{user.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // [Fact]
    // public async Task RemoveUser_ShouldReturnNullException_WhenUserDescriptorMapperReturnsNull()
    // {
    //     var userToBeDeletedId = Guid.NewGuid();
    //     var client = factory.CreateAuthenticatedClient(policyUser, config: builder =>
    //     {
    //         var mapper = Mock.Of<IUserDescriptorMapper>();
    //         Mock.Get(mapper)
    //             .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
    //             .Returns(value: null!);
    //
    //         builder.ConfigureTestServices(services => services.AddScoped(_ => mapper));
    //     });
    //     await Assert.ThrowsAsync<NullReferenceException>( () => client.DeleteAsync($"user/remove/{userToBeDeletedId}"));
    // }

    private User SetupAuthPolicyUser() =>
        new()
        {
            Roles = new List<Role>
            {
                new()
                {
                    Key = RoleKeys.AuthAdminKey, Name = "Auth", Id = Guid.NewGuid()
                }
            },
            AllowCprLookup = true,
            Name = Guid.NewGuid().ToString()
        };
}
