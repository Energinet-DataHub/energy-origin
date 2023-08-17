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
using WireMock.Server;

namespace Integration.Tests.Controllers;

public class TermsControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public TermsControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnUnauthorized_WhenInvokedWithoutAuthorization()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.PutAsync("terms/user/accept/1", null);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task AcceptCompanyAsync_ShouldReturnUnauthorized_WhenInvokedWithoutAuthorization()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.PutAsync("terms/company/accept/1", null);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnOkAndOnlyUpdateAcceptedUserTermsVersion_WhenUserExists()
    {
        var server = WireMockServer.Start();
        var options = new DataSyncOptions
        {
            Uri = new Uri($"http://localhost:{server.Port}/")
        };
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => options)));

        server.MockRelationsEndpoint();

        var result = await client.PutAsync("terms/user/accept/10", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserTerms).SingleOrDefault(x => x.Id == user.Id)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.Id, dbUser.Id);
        Assert.Equal(user.AllowCprLookup, dbUser.AllowCprLookup);
        Assert.Contains(dbUser.UserTerms, x => x.Type == UserTermsType.PrivacyPolicy);
        Assert.Contains(dbUser.UserTerms, x => x.AcceptedVersion == 10);
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnOkAndCreateUser_WhenUserDoesNotExist()
    {
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var user = new User
        {
            Id = null,
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 10 } },
            Company = null,
            CompanyId = null,
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = providerKeyType, UserProviderKey = providerKey } }
        };

        var server = WireMockServer.Start();
        var datasyncOptions = new DataSyncOptions
        {
            Uri = new Uri($"http://localhost:{server.Port}/")
        };

        var role = "default";
        var roleOptions = new RoleOptions()
        {
            RoleConfigurations = new() { new() { Key = role, Name = role, IsDefault = true } }
        };

        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services =>
        {
            services.AddScoped(_ => datasyncOptions);
            services.AddScoped(_ => roleOptions);
        }));

        server.MockRelationsEndpoint();

        var result = await client.PutAsync("terms/user/accept/10", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserTerms).Include(x => x.UserRoles).SingleOrDefault(x => x.Name == user.Name)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.AllowCprLookup, dbUser.AllowCprLookup);
        Assert.Contains(dbUser.UserTerms, x => x.Type == UserTermsType.PrivacyPolicy);
        Assert.Contains(dbUser.UserTerms, x => x.AcceptedVersion == 10);
        Assert.Contains(dbUser.UserRoles, x => x.Role == role);
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

        var response = await client.PutAsync("terms/user/accept/10", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
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

        var response = await client.PutAsync("terms/user/accept/10", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task AcceptCompanyAsync_ShouldReturnOkAndCreateCompanyTerms_WhenCompanyTermsDoesNotExist()
    {
        var user = await factory.AddUserToDatabaseAsync(new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company
            {
                Name = "TestCompany",
                Tin = "123123",
                CompanyTerms = new List<CompanyTerms> { new() { Type = CompanyTermsType.TermsOfService, AcceptedVersion = 9 } }
            },
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = ProviderKeyType.MitIdUuid, UserProviderKey = Guid.NewGuid().ToString() } }
        });
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.OrganizationAdmin);

        var result = await client.PutAsync("terms/company/accept/10", null);

        var company = factory.DataContext.Users.Include(x => x.Company).ThenInclude(x => x!.CompanyTerms).SingleOrDefault(x => x.Name == user.Name)!.Company!;
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains(company.CompanyTerms, x => x.Type == CompanyTermsType.TermsOfService);
        Assert.Contains(company.CompanyTerms, x => x.AcceptedVersion == 10);
    }

    [Fact]
    public async Task AcceptCompanyAsync_ShouldReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user);

        var response = await client.PutAsync("terms/company/accept/10", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AcceptCompanyAsync_ShouldReturnUnauthorized_WhenRequestIsAuthenticatedByPrivateUser()
    {
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.OrganizationAdmin);

        var response = await client.PutAsync("terms/company/accept/10", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
