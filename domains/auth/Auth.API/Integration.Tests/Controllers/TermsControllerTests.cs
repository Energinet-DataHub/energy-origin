using System.Net;
using API.Models.Entities;
using API.Options;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Relation.V1;
using WireMock.Server;
using Integration.Tests.Extensions;

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

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task AcceptUserTermsAsync_ShouldReturnOkAndOnlyUpdateAcceptedUserTermsVersion_WhenUserExists(int acceptedVersion)
    {
        var server = WireMockServer.Start();
        var options = new DataHubFacadeOptions()
        {
            Url = $"http://localhost:{server.Port}/"
        };
        var companyId = Guid.NewGuid();
        var user = await factory.AddUserToDatabaseAsync(new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company
            {
                Id = companyId,
                Tin = Guid.NewGuid().ToString(),
                Name = "Some Company"
            },
            CompanyId = companyId,
            UserTerms = new List<UserTerms> { new() { AcceptedVersion = 1 } }
        });

        var relationClient = Substitute.For<Relation.V1.Relation.RelationClient>();
        relationClient.CreateRelationAsync(Arg.Any<CreateRelationRequest>(), cancellationToken: CancellationToken.None)
            .Returns(new CreateRelationResponse
            {
                ErrorMessage = "",
                Success = true
            });
        var client = factory.CreateAuthenticatedClient(user, config: builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => relationClient);
                services.AddScoped(_ => options);
            });
        });

        server.MockRelationsEndpoint();

        var result = await client.PutAsync($"terms/user/accept/{acceptedVersion}", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserTerms).SingleOrDefault(x => x.Id == user.Id)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.Id, dbUser.Id);
        Assert.Equal(user.AllowCprLookup, dbUser.AllowCprLookup);
        Assert.Contains(dbUser.UserTerms, x => x.Type == UserTermsType.PrivacyPolicy && x.AcceptedVersion == acceptedVersion);
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnOkAndCreateUser_WhenUserDoesNotExist()
    {
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var companyId = Guid.NewGuid();
        var user = new User
        {
            Id = null,
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company { Tin = "12345678", Id = companyId },
            CompanyId = companyId,
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = providerKeyType, UserProviderKey = providerKey } }
        };

        var server = WireMockServer.Start();
        var options = new DataHubFacadeOptions()
        {
            Url = $"http://localhost:{server.Port}/"
        };

        var role = "default";
        var roleOptions = new RoleOptions()
        {
            RoleConfigurations = new() { new() { Key = role, Name = role, IsDefault = true } }
        };

        var relationClient = Substitute.For<Relation.V1.Relation.RelationClient>();
        relationClient.CreateRelationAsync(Arg.Any<CreateRelationRequest>(), cancellationToken: CancellationToken.None)
            .Returns(new CreateRelationResponse
            {
                ErrorMessage = "",
                Success = true
            });

        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(_ => relationClient);
            services.AddScoped(_ => options);
            services.AddScoped(_ => roleOptions);
        }));

        server.MockRelationsEndpoint();

        var result = await client.PutAsync("terms/user/accept/2", null);
        var dbUser = factory.DataContext.Users.Include(x => x.UserTerms).Include(x => x.UserRoles).SingleOrDefault(x => x.Name == user.Name)!;

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(user.Name, dbUser.Name);
        Assert.Equal(user.AllowCprLookup, dbUser.AllowCprLookup);
        Assert.Contains(dbUser.UserTerms, x => x is { Type: UserTermsType.PrivacyPolicy, AcceptedVersion: 2 });
        Assert.Contains(dbUser.UserRoles, x => x.Role == role);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AcceptUserTermsAsync_ShouldReturnOkAndCreateNewUserWithCompanyWithPredictedId_WhenIdGenerationIsPredictable(bool existingCompany)
    {
        var providerKey = Guid.NewGuid().ToString();
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company
            {
                Id = companyId,
                Name = "TestCompany",
                Tin = Guid.NewGuid().ToString(),
                CompanyTerms = new List<CompanyTerms> { new() { Type = CompanyTermsType.TermsOfService, AcceptedVersion = 1 } }
            },
            CompanyId = companyId,
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = providerKeyType, UserProviderKey = providerKey } }
        };

        if (existingCompany)
        {
            var dbContext = factory.DataContext;
            dbContext.Companies.Add(user.Company);
            await dbContext.SaveChangesAsync();
        }

        var relationClient = Substitute.For<Relation.V1.Relation.RelationClient>();
        relationClient.CreateRelationAsync(Arg.Any<CreateRelationRequest>(), cancellationToken: CancellationToken.None)
            .Returns(new CreateRelationResponse
            {
                ErrorMessage = "",
                Success = true
            });

        var client = factory.CreateAuthenticatedClient(user, config: builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => relationClient);
            });
            builder.UseSetting($"{OidcOptions.Prefix}:{nameof(OidcOptions.IdGeneration)}",
                nameof(OidcOptions.Generation.Predictable));
        });

        var result = await client.PutAsync("terms/user/accept/2", null);
        var dbCompany = factory.DataContext.Companies.SingleOrDefault(x => x.Id == companyId);
        var dbUser = factory.DataContext.Users.SingleOrDefault(x => x.Id == userId);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(dbCompany);
        Assert.NotNull(dbUser);
        Assert.NotEqual(companyId, dbUser.Id);
        Assert.Equal(companyId, dbUser.CompanyId);
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnOk_ForSimplestTestCase()
    {
        var user = await factory.AddUserToDatabaseAsync(new User
        {
            Name = Guid.NewGuid().ToString(),
            Company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "TestCompany",
                Tin = Guid.NewGuid().ToString()
            }
        });

        var relationClient = Substitute.For<Relation.V1.Relation.RelationClient>();
        relationClient.CreateRelationAsync(Arg.Any<CreateRelationRequest>(), cancellationToken: CancellationToken.None)
            .Returns(new CreateRelationResponse
            {
                ErrorMessage = "",
                Success = true
            });
        var client = factory.CreateAuthenticatedClient(user, config: builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => relationClient);
            });
        });

        var response = await client.PutAsync("terms/user/accept/2", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnBadRequestError_WhenAcceptingOlderVersion()
    {
        var user = await factory.AddUserToDatabaseAsync(new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            UserTerms = new List<UserTerms> { new() { AcceptedVersion = 3 } }
        });
        var client = factory.CreateAuthenticatedClient(user);

        var response = await client.PutAsync("terms/user/accept/2", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AcceptUserTermsAsync_ShouldReturnBadRequestError_WhenTermsVersionIsInvalid()
    {
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user);

        var response = await client.PutAsync("terms/user/accept/10", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task AcceptCompanyAsync_ShouldReturnOkAndCreateCompanyTerms_WhenCompanyTermsExists(int acceptedVersion)
    {
        var user = await factory.AddUserToDatabaseAsync(new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "TestCompany",
                Tin = Guid.NewGuid().ToString(),
                CompanyTerms = new List<CompanyTerms> { new() { Type = CompanyTermsType.TermsOfService, AcceptedVersion = 1 } }
            },
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = ProviderKeyType.MitIdUuid, UserProviderKey = Guid.NewGuid().ToString() } }
        });
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.OrganizationAdmin);

        var result = await client.PutAsync($"terms/company/accept/{acceptedVersion}", null);

        var company = factory.DataContext.Users.Include(x => x.Company).ThenInclude(x => x!.CompanyTerms).SingleOrDefault(x => x.Name == user.Name)!.Company!;
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains(company.CompanyTerms, x => x.Type == CompanyTermsType.TermsOfService && x.AcceptedVersion == acceptedVersion);
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
                Id = Guid.NewGuid(),
                Name = "TestCompany",
                Tin = Guid.NewGuid().ToString()
            },
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = ProviderKeyType.MitIdUuid, UserProviderKey = Guid.NewGuid().ToString() } }
        });
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.OrganizationAdmin);

        var result = await client.PutAsync("terms/company/accept/2", null);

        var company = factory.DataContext.Users.Include(x => x.Company).ThenInclude(x => x!.CompanyTerms).SingleOrDefault(x => x.Name == user.Name)!.Company!;
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains(company.CompanyTerms, x => x is { Type: CompanyTermsType.TermsOfService, AcceptedVersion: 2 });
    }

    [Fact]
    public async Task AcceptCompanyAsync_ShouldReturnBadRequestError_WhenAcceptingOlderVersion()
    {
        var user = await factory.AddUserToDatabaseAsync(new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "TestCompany",
                Tin = Guid.NewGuid().ToString(),
                CompanyTerms = new List<CompanyTerms> { new() { Type = CompanyTermsType.TermsOfService, AcceptedVersion = 3 } }
            },
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = ProviderKeyType.MitIdUuid, UserProviderKey = Guid.NewGuid().ToString() } }
        });
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.OrganizationAdmin);

        var response = await client.PutAsync("terms/company/accept/2", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AcceptCompanyAsync_ShouldReturnBadRequestError_WhenTermsVersionIsInvalid()
    {
        var user = await factory.AddUserToDatabaseAsync(new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = false,
            Company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "TestCompany",
                Tin = Guid.NewGuid().ToString()
            },
            UserProviders = new List<UserProvider> { new() { ProviderKeyType = ProviderKeyType.MitIdUuid, UserProviderKey = Guid.NewGuid().ToString() } }
        });
        var client = factory.CreateAuthenticatedClient(user, role: RoleKey.OrganizationAdmin);

        var response = await client.PutAsync("terms/company/accept/10", null);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
