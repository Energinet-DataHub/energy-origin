using System.Net;
using System.Text;
using API.Options;
using EnergyOrigin.TokenValidation.Models.Requests;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using API.Models.Entities;
using API.Values;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WireMock.Server;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Integration.Tests.Controllers;

public class RoleControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public RoleControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task AssignRole_ReturnsOk_WhenRoleIsAssigned()
    {
        var server = WireMockServer.Start();
        var options = Options.Create(new DataSyncOptions
        {
            Uri = new Uri($"http://localhost:{server.Port}/")
        });
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
        var newUser = new User
        {
            Roles = new List<Role>
            {
                new()
                {
                    Key = RoleKeys.AuthAdminKey, Name = "Auth", Id = Guid.NewGuid()
                }
            },
            AllowCprLookup = true, Name = Guid.NewGuid().ToString()
        };

        var user = await factory.AddUserToDatabaseAsync(newUser);
        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        server.MockRelationsEndpoint();

        var roleRequest = new RoleRequest
        {
            RoleKey = "testRoleKey",
            UserId = userGuid
        };
        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");
        var response = await client.PutAsync("role/assignRole", httpContent);
        var dbUser = factory.DataContext.Users.Include(x=>x.Roles).FirstOrDefault(x => x.Id == roleRequest.UserId)!;

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(roleRequest.RoleKey, dbUser.Roles.FirstOrDefault()?.Key);
    }


    [Fact]
    public async Task AssignRole_ThrowsException_WhenUserDoesNotExist()
    {
        var server = WireMockServer.Start();
        var options = Options.Create(new DataSyncOptions
        {
            Uri = new Uri($"http://localhost:{server.Port}/")
        });

        var nonExistentUserId = Guid.NewGuid();
        var roleRequest = new RoleRequest
        {
            RoleKey = "testRoleKey",
            UserId = nonExistentUserId
        };
        var newUser = new User
        {
            Roles = new List<Role>
            {
                new()
                {
                    Key = RoleKeys.AuthAdminKey, Name = "Auth", Id = Guid.NewGuid()
                }
            },
            AllowCprLookup = true, Name = Guid.NewGuid().ToString()
        };
        var user = await factory.AddUserToDatabaseAsync(newUser);
        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        var httpContent = new StringContent(JsonSerializer.Serialize(roleRequest), Encoding.UTF8, "application/json");

        await Assert.ThrowsAsync<NullReferenceException>(async () => await client.PutAsync("role/assignRole", httpContent));
    }
}
