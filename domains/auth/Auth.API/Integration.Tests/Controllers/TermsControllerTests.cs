using System.Net;
using System.Security.Claims;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.Server;

namespace Integration.Tests.Controllers;

public class TermsControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public TermsControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnNoContentAndOnlyUpdateAcceptedUserTermsVersion_WhenUserExists()
    {
        var server = WireMockServer.Start();
        var options = Options.Create(new DataSyncOptions
        {
            Uri = new Uri($"http://localhost:{server.Port}/")
        });
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        server.MockRelationsEndpoint();

        var result = await client.PutAsync("terms/user/accept/10", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserTerms).FirstOrDefault(x => x.Id == user.Id)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.Id, dbUser.Id);
        Assert.Equal(user.AllowCprLookup, dbUser.AllowCprLookup);
        Assert.Contains(dbUser.UserTerms, x => x.Type == UserTermsType.PrivacyPolicy);
        Assert.Contains(dbUser.UserTerms, x => x.AcceptedVersion == "10");
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnNoContentAndCreateUser_WhenUserDoesNotExist()
    {
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var user = new User
        {
            Id = null,
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = "10" } },
            Company = null,
            CompanyId = null,
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = providerKeyType, UserProviderKey = providerKey } }
        };

        var server = WireMockServer.Start();
        var options = Options.Create(new DataSyncOptions
        {
            Uri = new Uri($"http://localhost:{server.Port}/")
        });
        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        server.MockRelationsEndpoint();

        var result = await client.PutAsync("terms/user/acccept/10", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserTerms).FirstOrDefault(x => x.Name == user.Name)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.AllowCprLookup, dbUser.AllowCprLookup);
        Assert.Contains(dbUser.UserTerms, x => x.Type == UserTermsType.PrivacyPolicy);
        Assert.Contains(dbUser.UserTerms, x => x.AcceptedVersion == "10");
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var client = factory.CreateAuthenticatedClient(user, config: builder =>
        {
            var mapper = Mock.Of<IUserDescriptorMapper>();
            Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services => services.AddScoped(_ => mapper));
        });

        await Assert.ThrowsAsync<NullReferenceException>(() => client.PutAsync("terms/user/accept/10", null));
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnInternalServerError_WhenDescriptorIdExistsButUserCannotBeFound()
    {
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user, config: builder =>
        {
            var userService = Mock.Of<IUserService>();
            Mock.Get(userService)
                .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services => services.AddScoped(_ => userService));
        });

        await Assert.ThrowsAsync<NullReferenceException>(() => client.PutAsync("terms/user/accept/10", null));
    }

    [Fact]
    public async Task AcceptCompanyAsync_ShouldReturnNoContentAndCreateCompanyTerms_WhenCompanyTermsDoesNotExist()
    {
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var newUser = new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company
            {
                Name = "TestCompany",
                Tin = "123123",
                CompanyTerms = new List<CompanyTerms> { new() { Type = CompanyTermsType.TermsOfService, AcceptedVersion = "10" } }
            },
            Roles = new List<Role>
            {
                new()
                {
                    Key = RoleKeys.AuthAdminKey, Name = "AuthAdmin", IsDefault = false
                }
            },
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = providerKeyType, UserProviderKey = providerKey } }
        };
        var user = await factory.AddUserToDatabaseAsync(newUser);
        var client = factory.CreateAuthenticatedClient(user);

        var result = await client.PutAsync("terms/company/accept/10", null);
        var dbUser = factory.DataContext.Users.Include(x => x.Company).ThenInclude(x => x!.CompanyTerms).FirstOrDefault(x => x.Name == user.Name)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Contains(dbUser.Company!.CompanyTerms, x => x.Type == CompanyTermsType.TermsOfService);
        Assert.Contains(dbUser.Company.CompanyTerms, x => x.AcceptedVersion == "10");
    }

    [Fact]
    public async Task AcceptCompanyAsync_ShouldReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var newUser = new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company
            {
                Name = "TestCompany",
                Tin = "123123",
                CompanyTerms = new List<CompanyTerms> { new() { Type = CompanyTermsType.TermsOfService, AcceptedVersion = "10" } }
            },
            Roles = new List<Role>
            {
                new()
                {
                    Key = "test", Name = "test", IsDefault = false
                }
            },
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = providerKeyType, UserProviderKey = providerKey } }
        };
        var client = factory.CreateAuthenticatedClient(newUser);

        var response = await client.PutAsync("terms/company/accept/10", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
