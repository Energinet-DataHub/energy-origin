using System.Net;
using System.Security.Claims;
using System.Text;
using API.Models.Entities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Models.Requests;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Integration.Tests.Controllers;

public class RoleControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    private readonly User policyUser;
    public RoleControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
        policyUser = SetupAuthPolicyUser();
    }

    [Fact]
    public async Task AssignRole_ReturnsOk_WhenRoleIsAssigned()
    {
        var dbContext = factory.DataContext;
        var userGuid = Guid.NewGuid();
        await dbContext.Roles.AddAsync(new Role
        {
            Id = Guid.NewGuid(), Name = "TestRole", Key = "testRoleKey"
        });

        await dbContext.Users.AddAsync(new User()
        {
            Id = userGuid, Name = "TestUser", AllowCprLookup = false
        });
        await dbContext.SaveChangesAsync();
        var client = factory.CreateAuthenticatedClient(policyUser);

        var roleRequest = new RoleRequest
        {
            RoleKey = "testRoleKey",
            UserId = userGuid
        };
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");
        var response = await client.PutAsync("role/assignRole", httpContent);
        var dbUser = factory.DataContext.Users.Include(x => x.Roles).FirstOrDefault(x => x.Id == roleRequest.UserId)!;

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(roleRequest.RoleKey, dbUser.Roles.FirstOrDefault()?.Key);
    }

    [Fact]
    public async Task AssignRole_ShouldThrowNullException_WhenUserDoesNotExist()
    {
        var dbContext = factory.DataContext;
        var nonExistentUserId = Guid.NewGuid();
        await dbContext.Roles.AddAsync(new Role
        {
            Id = Guid.NewGuid(), Name = "TestRole", Key = "roleKey"
        });
        await dbContext.SaveChangesAsync();
        var roleRequest = new RoleRequest
        {
            RoleKey = "roleKey",
            UserId = nonExistentUserId
        };
        var client = factory.CreateAuthenticatedClient(policyUser);
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");

        await Assert.ThrowsAsync<NullReferenceException>(async () => await client.PutAsync("role/assignRole", httpContent));
    }

    [Fact]
    public async Task AssignRole_ShouldThrowNullException_WhenRoleDoesNotExist()
    {
        var nonExistentRoleKey = "notExistentRoleKey";
        var userGuid = Guid.NewGuid();
        await factory.AddUserToDatabaseAsync(new User
        {
            Id = userGuid, Name = "TestUser", AllowCprLookup = false
        });
        var roleRequest = new RoleRequest
        {
            RoleKey = nonExistentRoleKey,
            UserId = userGuid
        };
        var client = factory.CreateAuthenticatedClient(policyUser);

        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");

        await Assert.ThrowsAsync<NullReferenceException>(async () => await client.PutAsync("role/assignRole", httpContent));
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldRemoveRole_WhenRoleExists()
    {
        var existingRoleKey = "existingRoleKey";
        var existingUserId = Guid.NewGuid();
        var dataContext = factory.DataContext;
        var userWithRole = new User
        {
            Id = existingUserId,
            Roles = new List<Role>
            {
                new()
                {
                    Key = existingRoleKey, Name = "ExistingRole", Id = Guid.NewGuid()
                }
            },
            AllowCprLookup = false,
            Name = "TestUser"
        };
        await dataContext.AddAsync(userWithRole);
        await dataContext.SaveChangesAsync();
        var roleRequest = new RoleRequest
        {
            RoleKey = existingRoleKey,
            UserId = existingUserId
        };

        var policyAuthUser = SetupAuthPolicyUser();
        var client = factory.CreateAuthenticatedClient(policyAuthUser);
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");

        var response = await client.PutAsync("role/removeRoleFromUser", httpContent);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedUser = factory.DataContext.Users.FirstOrDefault(x => x.Id == userWithRole.Id);
        Assert.DoesNotContain(updatedUser!.Roles, role => role.Key == existingRoleKey);
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldThrowException_WhenRoleDoesNotExist()
    {
        var nonExistentRoleKey = "notExistentRoleKey";
        var existingUserId = Guid.NewGuid();
        var dataContext = factory.DataContext;
        var userWithRole = new User
        {
            Id = existingUserId,
            AllowCprLookup = false,
            Name = "TestUser"
        };
        await dataContext.AddAsync(userWithRole);
        await dataContext.SaveChangesAsync();
        var roleRequest = new RoleRequest
        {
            RoleKey = nonExistentRoleKey,
            UserId = existingUserId
        };

        var policyAuthUser = SetupAuthPolicyUser();
        var client = factory.CreateAuthenticatedClient(policyAuthUser);
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");

        await Assert.ThrowsAsync<NullReferenceException>(async () => await client.PutAsync("role/removeRoleFromUser", httpContent));
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldThrowException_WhenUserDoesNotExist()
    {
        var dbContext = factory.DataContext;
        var nonExistentUserId = Guid.NewGuid();
        await dbContext.Roles.AddAsync(new Role
        {
            Id = Guid.NewGuid(), Name = "TestRole", Key = "userRoleKey"
        });
        await dbContext.SaveChangesAsync();
        var roleRequest = new RoleRequest
        {
            RoleKey = "userRoleKey",
            UserId = nonExistentUserId
        };

        var policyAuthUser = SetupAuthPolicyUser();
        var client = factory.CreateAuthenticatedClient(policyAuthUser);
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");

        await Assert.ThrowsAsync<NullReferenceException>(async () => await client.PutAsync("role/removeRoleFromUser", httpContent));
    }

    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnBadRequest_WhenUserTriesToRemoveAdminFromThemselves()
    {
        await factory.AddUserToDatabaseAsync(policyUser);

        var roleRequest = new RoleRequest
        {
            RoleKey = RoleKeys.AuthAdminKey,
            UserId = policyUser.Id ?? new Guid()
        };

        var client = factory.CreateAuthenticatedClient(policyUser);
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");
        var response = await client.PutAsync("role/removeRoleFromUser", httpContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Theory]
    [InlineData("role/assignRole")]
    [InlineData("role/removeRoleFromUser")]
    public async Task RoleCalls_ShouldReturnForbidden_WhenNonAdminUser(string routePath)
    {
        var roleRequest = new RoleRequest
        {
            RoleKey = "testRoleKey",
            UserId = Guid.NewGuid()
        };

        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user);
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");

        var response = await client.PutAsync(routePath, httpContent);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("role/assignRole")]
    [InlineData("role/removeRoleFromUser")]
    public async Task RoleCalls_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull(string routePath)
    {
        var client = factory.CreateAuthenticatedClient(policyUser, config: builder =>
        {
            var mapper = Mock.Of<IUserDescriptorMapper>();
            Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services => services.AddScoped(_ => mapper));
        });

        var roleRequest = new RoleRequest
        {
            RoleKey = "userRoleKey",
            UserId = Guid.NewGuid()
        };
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");

        await Assert.ThrowsAsync<NullReferenceException>(() => client.PutAsync(routePath, httpContent));
    }

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
