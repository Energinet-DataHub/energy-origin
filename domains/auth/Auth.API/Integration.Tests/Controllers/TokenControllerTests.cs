using System.IdentityModel.Tokens.Jwt;
using System.Net;
using API.Models.Entities;
using API.Options;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Controllers;

public class TokenControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public TokenControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task RefreshAsync_ShouldReturnUnauthorized_WhenInvokedWithoutAuthorization()
    {
        var client = factory.CreateAnonymousClient();

        var result = await client.GetAsync("auth/token");

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnTokenWithSameScope_WhenTermsVersionHasIncreasedDuringCurrentLogin()
    {
        var earlier = DateTimeOffset.Now.AddSeconds(-1);
        var user = await factory.AddUserToDatabaseAsync(new User { Id = Guid.NewGuid(), Name = "TestUser", AllowCprLookup = false, UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 1 } } });
        var oldClient = factory.CreateAuthenticatedClient(user, issueAt: earlier.UtcDateTime, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => new TermsOptions()
        {
            PrivacyPolicyVersion = 1,
            TermsOfServiceVersion = 1
        })));
        var oldToken = oldClient.DefaultRequestHeaders.Authorization?.Parameter;

        var client = factory.CreateAuthenticatedClient(user, config: builder => builder.ConfigureTestServices(services => services.AddScoped(_ => new TermsOptions()
        {
            PrivacyPolicyVersion = 2,
            TermsOfServiceVersion = 2
        })));
        var result = await client.GetAsync("auth/token");
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var newToken = await result.Content.ReadAsStringAsync();
        Assert.NotNull(newToken);
        Assert.NotEqual(oldToken, newToken);

        var oldScope = new JwtSecurityTokenHandler().ReadJwtToken(oldToken).Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        var newScope = new JwtSecurityTokenHandler().ReadJwtToken(newToken).Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        Assert.NotNull(oldScope);
        Assert.NotNull(newScope);
        Assert.Equal(oldScope, newScope);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnTokenWithDifferentScope_WhenTermsVersionHasIncreasedSinceLastLogin()
    {
        var newUser = new User { Id = Guid.NewGuid(), Company = new Company { Name = "test", Tin = "grgrgr" }, Name = "TestUser", AllowCprLookup = false, UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 3 } } };
        var user = await factory.AddUserToDatabaseAsync(newUser);
        user.UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 4 } };

        var client = factory.CreateAuthenticatedClient(user);
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;

        var result = await client.GetAsync("auth/token");
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var newToken = await result.Content.ReadAsStringAsync();
        Assert.NotNull(newToken);
        Assert.NotEqual(oldToken, newToken);

        var oldScope = new JwtSecurityTokenHandler().ReadJwtToken(oldToken).Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        var newScope = new JwtSecurityTokenHandler().ReadJwtToken(newToken).Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        Assert.NotNull(oldScope);
        Assert.NotNull(newScope);
        Assert.NotEqual(oldScope, newScope);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnTokenWithTermsScope_WhenUserRefreshesWithUnacceptedTerms()
    {
        var user = new User()
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Company = new Company { Name = "test", Tin = "rerere" }
        };
        var client = factory.CreateAuthenticatedClient(user, issueAt: DateTime.UtcNow.AddMinutes(-1));
        user.UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 3 } };
        await factory.AddUserToDatabaseAsync(user);
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;
        var result = await client.GetAsync("auth/token");
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var newToken = await result.Content.ReadAsStringAsync();

        Assert.NotNull(newToken);
        Assert.NotEqual(oldToken, newToken);

        var oldScope = new JwtSecurityTokenHandler().ReadJwtToken(oldToken).Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        var newScope = new JwtSecurityTokenHandler().ReadJwtToken(newToken).Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        Assert.NotNull(oldScope);
        Assert.NotNull(newScope);
        Assert.Equal(UserScopeName.NotAcceptedPrivacyPolicy, oldScope);
        Assert.DoesNotContain(UserScopeName.NotAcceptedPrivacyPolicy, newScope);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnTokenWithTermsScope_WhenUserHasNotAcceptedTermsPreviously()
    {
        var user = new User()
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString()
        };
        var client = factory.CreateAuthenticatedClient(user, issueAt: DateTime.UtcNow.AddMinutes(-1));
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;
        var result = await client.GetAsync("auth/token");
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var newToken = await result.Content.ReadAsStringAsync();

        Assert.NotNull(newToken);
        Assert.NotEqual(oldToken, newToken);

        var oldScope = new JwtSecurityTokenHandler().ReadJwtToken(oldToken).Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        var newScope = new JwtSecurityTokenHandler().ReadJwtToken(newToken).Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        Assert.NotNull(oldScope);
        Assert.NotNull(newScope);
        Assert.Equal(UserScopeName.NotAcceptedPrivacyPolicy, oldScope);
        Assert.Equal(UserScopeName.NotAcceptedPrivacyPolicy, newScope);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnTokenWithUpdatedOrganizationId_WhenOrganizationHasBeenCreated()
    {
        var user = new User()
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Company = new Company()
            {
                Name = Guid.NewGuid().ToString(),
                Tin = Guid.NewGuid().ToString(),
            }
        };
        var client = factory.CreateAuthenticatedClient(user, issueAt: DateTime.UtcNow.AddMinutes(-1));
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;

        await factory.AddUserToDatabaseAsync(user);

        var result = await client.GetAsync("auth/token");

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var newToken = await result.Content.ReadAsStringAsync();
        Assert.NotNull(newToken);
        Assert.NotEqual(oldToken, newToken);

        var oldId = new JwtSecurityTokenHandler().ReadJwtToken(oldToken).Claims.First(x => x.Type == UserClaimName.OrganizationId)!.Value;
        var newId = new JwtSecurityTokenHandler().ReadJwtToken(newToken).Claims.First(x => x.Type == UserClaimName.OrganizationId)!.Value;
        var subject = new JwtSecurityTokenHandler().ReadJwtToken(newToken).Claims.First(x => x.Type == UserClaimName.Subject)!.Value;
        var userId = new JwtSecurityTokenHandler().ReadJwtToken(newToken).Claims.First(x => x.Type == UserClaimName.Actor)!.Value;
        Assert.NotNull(oldId);
        Assert.NotNull(newId);
        Assert.Equal(Guid.Empty.ToString(), oldId);
        Assert.Equal(user.Company.Id.ToString(), newId);
        Assert.Equal(subject, newId);
        Assert.Equal(user.Id.ToString(), userId);
    }
}
