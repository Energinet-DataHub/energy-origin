using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using API.Models;
using API.Options;
using API.Services;
using API.Utilities;
using Microsoft.IdentityModel.Tokens;

namespace Tests.Utilities;

public class TokenIssuerTests
{
    private readonly IUserService service = Mock.Of<IUserService>();

    [Fact]
    public async Task IssueAsync_ShouldReturnATokenForThatUser_WhenIssuingForAUser()
    {
        var userId = AddUser();

        var token = await GetTokenIssuer().IssueAsync(userId.ToString());

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(userId.ToString(), jwt.Subject);
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnATokenWithCorrectValidatityTimes_WhenIssuingAtASpecifiedTime()
    {
        var userId = AddUser();
        var duration = new TimeSpan(10, 11, 12);
        var options = TestOptions.Token(duration: duration);
        var issueAt = new DateTime(2000, 1, 1, 0, 0, 0);

        var token = await GetTokenIssuer(tokenOptions: options.Value).IssueAsync(userId.ToString(), issueAt);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issueAt, jwt.ValidFrom);
        Assert.Equal(issueAt.Add(duration), jwt.ValidTo);
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnATokenCreatedUsingOptions_WhenIssuing()
    {
        var userId = AddUser();
        var audience = Guid.NewGuid().ToString();
        var issuer = Guid.NewGuid().ToString();
        var options = TestOptions.Token(audience, issuer);

        var token = await GetTokenIssuer(tokenOptions: options.Value).IssueAsync(userId.ToString());

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issuer, jwt.Issuer);
        Assert.Contains(audience, jwt.Audiences);
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnASignedToken_WhenIssuing()
    {
        var userId = AddUser();
        var options = TestOptions.Token();

        var token = await GetTokenIssuer(tokenOptions: options.Value).IssueAsync(userId.ToString());

        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.Value.PublicKeyPem));
        var key = new RsaSecurityKey(rsa);

        var parameters = new TokenValidationParameters()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateActor = false,
            ValidateLifetime = false,
            ValidateTokenReplay = false,
            IssuerSigningKey = key
        };
        new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var validatedToken);

        Assert.NotNull(validatedToken);
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnATokenWithUsersProperties_WhenIssuingForAUser()
    {
        var name = Guid.NewGuid().ToString();
        var tin = Guid.NewGuid().ToString();
        var version = Random.Shared.Next();
        var userId = AddUser(name, version, tin);

        var token = await GetTokenIssuer().IssueAsync(userId.ToString());

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(name, jwt.Claims.First(it => it.Type == "name")?.Value);
        Assert.Equal(tin, jwt.Claims.First(it => it.Type == "tin")?.Value);
        Assert.Equal($"{version}", jwt.Claims.First(it => it.Type == "terms")?.Value);
    }

    [Fact]
    public async Task IssueAsync_ShouldThrowKeyNotFoundException_WhenIssuingForNonExistingUser() => await Assert.ThrowsAsync<KeyNotFoundException>(async () => await GetTokenIssuer().IssueAsync(Guid.NewGuid().ToString()));

    private TokenIssuer GetTokenIssuer(TermsOptions? termsOptions = default, TokenOptions? tokenOptions = default) => new(termsOptions ?? TestOptions.Terms().Value, tokenOptions ?? TestOptions.Token().Value, service);

    private Guid AddUser(string? name = default, int version = 1, string? tin = default)
    {
        var id = Guid.NewGuid();
        Mock.Get(service)
            .Setup(it => it.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: new User()
            {
                Id = id,
                ProviderId = Guid.NewGuid().ToString(),
                Name = name ?? "Amigo",
                AcceptedTermsVersion = version,
                Tin = tin,
                AllowCPRLookup = true
            });
        return id;
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
