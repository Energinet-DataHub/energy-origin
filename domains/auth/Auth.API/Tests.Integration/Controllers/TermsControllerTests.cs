using System.Net;
using System.Security.Claims;
using System.Text;
using API.Models.DTOs;
using API.Repositories.Data;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;

namespace Tests.Integration.Controllers;

public class TermsControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public TermsControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnNoContentAndOnlyUpdateAcceptedTermsVersion_WhenUserExists()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var client = await factory.CreateAuthenticatedClientAsync(user);

        var dto = new AcceptTermsDTO(2);
        var httpContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);

        var dbUser = factory.ServiceProvider.GetRequiredService<DataContext>().Users.AsNoTracking().SingleOrDefault(x => x.Id == user.Id)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(user.ProviderId, dbUser.ProviderId);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.Tin, dbUser.Tin);
        Assert.Equal(user.Id, dbUser.Id);
        Assert.Equal(user.AllowCPRLookup, dbUser.AllowCPRLookup);
        Assert.Equal(dto.Version, dbUser.AcceptedTermsVersion);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnNoContentAndCreateUser_WhenUserDoesNotExist()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var name = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid().ToString();
        var tin = null! as string;
        var allowCprLookup = false;
        var acceptedTermsVersion = 0;

        var client = await factory.CreateAuthenticatedClientAsync(user, config: builder =>
        {
            var mapper = Mock.Of<IUserDescriptMapper>();
            _ = Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: new UserDescriptor(null!)
                {
                    Id = null,
                    Name = name,
                    ProviderId = providerId,
                    Tin = tin,
                    AllowCPRLookup = allowCprLookup,
                    AcceptedTermsVersion = acceptedTermsVersion
                });

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(x => mapper);
            });
        });

        var dto = new AcceptTermsDTO(1);
        var httpContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);

        var dbUser = factory.ServiceProvider.GetRequiredService<DataContext>().Users.AsNoTracking().SingleOrDefault(x => x.ProviderId == providerId)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(providerId, dbUser.ProviderId);
        Assert.Equal(name, dbUser.Name);
        Assert.Equal(tin, dbUser.Tin);
        Assert.Equal(allowCprLookup, dbUser.AllowCPRLookup);
        Assert.Equal(dto.Version, dbUser.AcceptedTermsVersion);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var client = await factory.CreateAuthenticatedClientAsync(user, config: builder =>
        {
            var mapper = Mock.Of<IUserDescriptMapper>();
            _ = Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(x => mapper);
            });
        });

        var dto = new AcceptTermsDTO(2);
        var httpContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnInternalServerError_WhenDescriptorIdExistsButUserCannotBeFound()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var client = await factory.CreateAuthenticatedClientAsync(user, config: builder =>
        {
            var userService = Mock.Of<IUserService>();
            _ = Mock.Get(userService)
                .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(x => userService);
            });
        });

        var dto = new AcceptTermsDTO(2);
        var httpContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
