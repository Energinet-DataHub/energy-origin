using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using API.Models.Entities;
using API.Services;
using API.Utilities;
using API.Values;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration;

namespace Integration.Tests.Controllers;

public class TokenControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public TokenControllerTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task RefreshAsync_ShouldReturnTokenWithSameScope_WhenInvokedAfterLoginWithExistingScope(int version)
    {
        var user = await factory.AddUserToDatabaseAsync();
        var client = factory.CreateAuthenticatedClient(user);
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;

        user.AcceptedTermsVersion = version;

        var context = factory.DataContext;
        context.Users.Update(user);
        context.SaveChanges();

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
        var user = await factory.AddUserToDatabaseAsync();
        user.AcceptedTermsVersion = 0;
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
    public async Task RefreshAsync_ShouldReturnTokenWithTermsScope_WhenUserHasNotAcceptedTermsPreviously()
    {
        var user = new User()
        {
            Id = null,
            Name = Guid.NewGuid().ToString()
        };
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
        Assert.Equal(UserScopeClaim.NotAcceptedTerms, oldScope);
        Assert.Equal(UserScopeClaim.NotAcceptedTerms, newScope);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnInternalServerError_WhenUserDescriptMapperReturnsNull()
    {
        var user = await factory.AddUserToDatabaseAsync();

        var client = factory.CreateAuthenticatedClient(user, config: builder =>
        {
            var mapper = Mock.Of<IClaimsWrapperMapper>();
            _ = Mock.Get(mapper)
                .Setup(x => x.Map(It.IsAny<ClaimsPrincipal>()))
                .Returns(value: null!);

            builder.ConfigureTestServices(services => services.AddScoped(x => mapper));
        });

        var result = await client.GetAsync("auth/token");

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnInternalServerError_WhenClaimsWrapperIdExistsButUserCannotBeFound()
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

        var result = await client.GetAsync("auth/token");

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
