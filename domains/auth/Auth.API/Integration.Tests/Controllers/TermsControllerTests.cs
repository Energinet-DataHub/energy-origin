using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Models.DTOs;
using API.Models.Entities;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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

        var client = factory.CreateAuthenticatedClient(user);

        var dto = new AcceptTermsDTO(2);
        var httpContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);

        var dbUser = factory.DataContext.Users.SingleOrDefault(x => x.Id == user.Id)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(user.ProviderId, dbUser.ProviderId);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.Id, dbUser.Id);
        Assert.Equal(user.AllowCPRLookup, dbUser.AllowCPRLookup);
        Assert.Equal(dto.Version, dbUser.AcceptedTermsVersion);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnNoContentAndCreateUser_WhenUserDoesNotExist()
    {
        var user = new User()
        {
            Id = null,
            Name = Guid.NewGuid().ToString(),
            ProviderId = Guid.NewGuid().ToString(),
            AllowCPRLookup = false,
            AcceptedTermsVersion = 0
        };

        var client = factory.CreateAuthenticatedClient(user);

        var dto = new AcceptTermsDTO(1);
        var httpContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);

        var dbUser = factory.DataContext.Users.SingleOrDefault(x => x.ProviderId == user.ProviderId)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(user.ProviderId, dbUser.ProviderId);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.AllowCPRLookup, dbUser.AllowCPRLookup);
        Assert.Equal(dto.Version, dbUser.AcceptedTermsVersion);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var client = factory.CreateAuthenticatedClient(user, config: (Action<Microsoft.AspNetCore.Hosting.IWebHostBuilder>?)(builder =>
        {
            var mapper = Mock.Of<API.Utilities.IUserDescriptorMapper>();
            _ = Mock.Get<API.Utilities.IUserDescriptorMapper>((API.Utilities.IUserDescriptorMapper)mapper)
                .Setup<UserDescriptor>(x => (UserDescriptor?)x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: null!);

            builder.ConfigureTestServices((Action<IServiceCollection>)(services => services.AddScoped<API.Utilities.IUserDescriptorMapper>((Func<IServiceProvider, API.Utilities.IUserDescriptorMapper>)(x => (API.Utilities.IUserDescriptorMapper)mapper))));
        }));

        var dto = new AcceptTermsDTO(2);
        var httpContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnInternalServerError_WhenDescriptorIdExistsButUserCannotBeFound()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var client = factory.CreateAuthenticatedClient(user, config: builder =>
        {
            var userService = Mock.Of<IUserService>();
            _ = Mock.Get(userService)
                .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services => services.AddScoped(x => userService));
        });

        var dto = new AcceptTermsDTO(2);
        var httpContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
