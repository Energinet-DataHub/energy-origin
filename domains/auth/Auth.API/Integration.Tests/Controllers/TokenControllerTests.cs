using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using API.Models.Entities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Controllers;

public class TokenControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public TokenControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("privacy0")]
    [InlineData("privacy1")]
    public async Task RefreshAsync_ShouldReturnTokenWithSameScope_WhenInvokedAfterLoginWithExistingScope(string termsVersion)
    {
        var newUser = new User {Id = Guid.NewGuid(), Name = "TestUser", AllowCprLookup = false, UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = termsVersion} } };
        var user = await factory.AddUserToDatabaseAsync(newUser);
        var client = factory.CreateAuthenticatedClient(user);
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;

        var context = factory.DataContext;
        context.Users.Update(user);
        await context.SaveChangesAsync();

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
        var newUser = new User {Id = Guid.NewGuid(), Name = "TestUser", AllowCprLookup = false, UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = "3" } } };
        var user = await factory.AddUserToDatabaseAsync(newUser);
        user.UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = "4" } };

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
            Name = Guid.NewGuid().ToString()
        };
        var client = factory.CreateAuthenticatedClient(user, issueAt: DateTime.UtcNow.AddMinutes(-1));
        user.UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = "3" } };
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
        Assert.Equal(UserScopeClaim.NotAcceptedPrivacyPolicyTerms + " " + UserScopeClaim.NotAcceptedTermsOfServiceTerms, oldScope);
        Assert.DoesNotContain(UserScopeClaim.NotAcceptedPrivacyPolicyTerms, newScope);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull()
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

        await Assert.ThrowsAsync<NullReferenceException>(() => client.GetAsync("auth/token"));
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
        Assert.Equal(UserScopeClaim.NotAcceptedPrivacyPolicyTerms + " " + UserScopeClaim.NotAcceptedTermsOfServiceTerms, oldScope);
        Assert.Equal(UserScopeClaim.NotAcceptedPrivacyPolicyTerms + " " + UserScopeClaim.NotAcceptedTermsOfServiceTerms, newScope);
    }
}
