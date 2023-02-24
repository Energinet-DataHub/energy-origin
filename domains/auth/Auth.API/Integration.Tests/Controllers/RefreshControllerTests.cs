using System.Net;
using Tests.Integration;
using System.IdentityModel.Tokens.Jwt;

namespace Integration.Tests.Controllers;

public class RefreshControllerTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;
    public RefreshControllerTests(AuthWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRedirectToAuthority_WhenInvoked()
    {
        var user = await factory.AddUserToDatabaseAsync();
        var tokenIssuedAt = DateTime.UtcNow.AddMinutes(-5);
        var client = factory.CreateAuthenticatedClient(user, issueAt: tokenIssuedAt);
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;

        var result = await client.GetAsync("auth/refresh");
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

        var header = result.Headers.GetValues("Set-Cookie");
        Assert.True(header.Count() >= 1);
        Assert.Contains("Authentication=", header.First());
        Assert.Contains("; secure", header.First());
        Assert.Contains("; expires=", header.First());

        var newToken = header.First().Split("Authentication=").Last().Split(";").First();
        Assert.NotEqual(oldToken, newToken);

        var oldTokenJwt = new JwtSecurityTokenHandler().ReadJwtToken(oldToken);
        var newTokenJwt = new JwtSecurityTokenHandler().ReadJwtToken(newToken);
        Assert.True(oldTokenJwt.ValidFrom < newTokenJwt.ValidFrom);
        Assert.True(oldTokenJwt.ValidTo < newTokenJwt.ValidTo);
    }
}
