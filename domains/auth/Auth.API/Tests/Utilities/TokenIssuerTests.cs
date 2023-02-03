using System.IdentityModel.Tokens.Jwt;
using API.Utilities;

namespace Tests.Utilities;

public class TokenIssuerTests
{
    [Fact]
    public void Issue_ShouldReturnATokenForThatUser_WhenIssuingForAUser()
    {
        var options = TestOptions.Token();
        var userId = "a-user-id";

        var token = TokenIssuer.Issue(options.Value, userId);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(userId, jwt.Subject);
    }

    [Fact]
    public void Issue_ShouldReturnATokenWithCorrectValidatityTimes_WhenIssuingAtASpecifiedTime()
    {
        var duration = new TimeSpan(10, 11, 12);
        var options = TestOptions.Token(duration: duration);
        var userId = "a-user-id";
        var issueAt = new DateTime(2000, 1, 1, 0, 0, 0);

        var token = TokenIssuer.Issue(options.Value, userId, issueAt);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issueAt, jwt.ValidFrom);
        Assert.Equal(issueAt.Add(duration), jwt.ValidTo);
    }

    [Fact]
    public void Issue_ShouldReturnATokenCreatedUsingOptions_WhenIssuing()
    {
        var audience = Guid.NewGuid().ToString();
        var issuer = Guid.NewGuid().ToString();
        var options = TestOptions.Token(audience, issuer);

        var token = TokenIssuer.Issue(options.Value, "a-user-id");

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issuer, jwt.Issuer);
        Assert.Contains(audience, jwt.Audiences);
    }

    private static JwtSecurityToken? Convert(string? token)
    {
        if (token == null)
        {
            return null;
        }
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(token);
    }
}
