using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Models.Requests;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.Server;

namespace Integration.Tests.Controllers;

public class TermsControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public TermsControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnNoContentAndOnlyUpdateAcceptedTermsVersion_WhenUserExists()
    {
        var server = WireMockServer.Start();
        var options = Options.Create(new DataSyncOptions
        {
            Uri = new Uri($"http://localhost:{server.Port}/")
        });
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        server.MockRelationsEndpoint();

        var dto = new AcceptTermsRequest(2);
        var httpContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);
        var dbUser = factory.DataContext.Users.FirstOrDefault(x => x.Id == user.Id)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.Id, dbUser.Id);
        Assert.Equal(user.AllowCprLookup, dbUser.AllowCprLookup);
        Assert.Equal(dto.Version, dbUser.AcceptedTermsVersion);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnNoContentAndCreateUser_WhenUserDoesNotExist()
    {
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitID_UUID;
        var user = new User
        {
            Id = null,
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            AcceptedTermsVersion = 0,
            Company = null,
            CompanyId = null,
            UserProviders = new List<UserProvider>() { new UserProvider() { ProviderKeyType = providerKeyType, UserProviderKey = providerKey } }
        };

        var server = WireMockServer.Start();
        var options = Options.Create(new DataSyncOptions
        {
            Uri = new Uri($"http://localhost:{server.Port}/")
        });
        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        server.MockRelationsEndpoint();

        var dto = new AcceptTermsRequest(1);
        var httpContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        var result = await client.PutAsync("terms/accept", httpContent);
        var dbUser = factory.DataContext.Users.FirstOrDefault(x => x.Name == user.Name)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(user.Name.ToString(), dbUser.Name);
        Assert.Equal(user.AllowCprLookup, dbUser.AllowCprLookup);
        Assert.Equal(dto.Version, dbUser.AcceptedTermsVersion);
    }

    [Fact]
    public async Task AcceptTermsAsync_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var client = factory.CreateAuthenticatedClient(user, config: builder =>
        {
            var mapper = Mock.Of<IUserDescriptorMapper>();
            _ = Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services => services.AddScoped(_ => mapper));
        });

        var dto = new AcceptTermsRequest(2);
        var httpContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

        await Assert.ThrowsAsync<NullReferenceException>(() => client.PutAsync("terms/accept", httpContent));
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

            builder.ConfigureTestServices(services => services.AddScoped(_ => userService));
        });

        var dto = new AcceptTermsRequest(2);
        var httpContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

        await Assert.ThrowsAsync<NullReferenceException>(() => client.PutAsync("terms/accept", httpContent));
    }
}
