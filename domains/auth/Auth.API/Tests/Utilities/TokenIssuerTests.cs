using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using API.Models;
using API.Options;
using API.Services;
using API.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Tests.Utilities;

public class TokenIssuerTests
{
    private readonly IUserService service = Mock.Of<IUserService>();

    private readonly TermsOptions termsOptions;
    private readonly TokenOptions tokenOptions;

    public TokenIssuerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        termsOptions = configuration.GetSection(TermsOptions.Prefix).Get<TermsOptions>()!;
        tokenOptions = configuration.GetSection(TokenOptions.Prefix).Get<TokenOptions>()!;
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnATokenForThatUser_WhenIssuingForAUser()
    {
        var descriptor = PrepareUser();

        var token = await GetTokenIssuer().IssueAsync(descriptor);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(descriptor.Id.ToString(), jwt.Subject);
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnATokenWithCorrectValidatityTimes_WhenIssuingAtASpecifiedTime()
    {
        var descriptor = PrepareUser();
        var duration = new TimeSpan(10, 11, 12);
        var options = TestOptions.Token(tokenOptions, duration: duration);
        var issueAt = new DateTime(2000, 1, 1, 0, 0, 0);

        var token = await GetTokenIssuer(token: options.Value).IssueAsync(descriptor, issueAt);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issueAt, jwt.ValidFrom);
        Assert.Equal(issueAt.Add(duration), jwt.ValidTo);
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnATokenCreatedUsingOptions_WhenIssuing()
    {
        var descriptor = PrepareUser();
        var audience = Guid.NewGuid().ToString();
        var issuer = Guid.NewGuid().ToString();
        var options = TestOptions.Token(tokenOptions, audience, issuer);

        var token = await GetTokenIssuer(token: options.Value).IssueAsync(descriptor);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issuer, jwt.Issuer);
        Assert.Contains(audience, jwt.Audiences);
    }

    [Fact]
    public async Task IssueAsync_ShouldReturnASignedToken_WhenIssuing()
    {
        var descriptor = PrepareUser();
        var options = TestOptions.Token(tokenOptions);

        var token = await GetTokenIssuer(token: options.Value).IssueAsync(descriptor);

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
        var accesToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var version = Random.Shared.Next();
        var descriptor = PrepareUser(name, version, tin, accesToken, identityToken);

        var token = await GetTokenIssuer().IssueAsync(descriptor);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(descriptor.Id?.ToString(), jwt.Claims.FirstOrDefault(it => it.Type == JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(name, jwt.Claims.FirstOrDefault(it => it.Type == JwtRegisteredClaimNames.Name)?.Value);
        Assert.Equal(tin, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.Tin)?.Value);
        Assert.Equal($"{version}", jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.TermsVersion)?.Value);
        Assert.Equal(descriptor.ProviderId, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.ProviderId)?.Value);
        Assert.Equal(descriptor.AllowCPRLookup, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.AllowCPRLookup)?.Value == "true");
        Assert.Equal(!descriptor.AllowCPRLookup, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.AllowCPRLookup)?.Value == "false");
        Assert.Equal(accesToken, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.AccessToken)?.Value);
        Assert.Equal(identityToken, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.IdentityToken)?.Value);
    }

    [Fact]
    public async Task IssueAsync_ShouldThrowKeyNotFoundException_WhenIssuingForNonExistingUser() => await Assert.ThrowsAsync<KeyNotFoundException>(async () => await GetTokenIssuer().IssueAsync(PrepareUser(addToMock: false)));

    [Fact]
    public async Task IssueAsync_ShouldReturnAToken_WhenIssuingForAnUnsavedUser()
    {
        var descriptor = PrepareUser(addToMock: false, hasId: false);

        var token = await GetTokenIssuer().IssueAsync(descriptor);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Null(jwt.Claims.FirstOrDefault(it => it.Type == JwtRegisteredClaimNames.Sub)?.Value);
    }

    private TokenIssuer GetTokenIssuer(TermsOptions? terms = default, TokenOptions? token = default) => new( Options.Create( terms ?? termsOptions), Options.Create(token ?? tokenOptions), service);

    private UserDescriptor PrepareUser(string? name = default, int version = 1, string? tin = default, string? accesToken = default, string? identityToken = default, bool addToMock = true, bool hasId = true)
    {
        var user = new User()
        {
            Id = hasId ? Guid.NewGuid() : null,
            ProviderId = Guid.NewGuid().ToString(),
            Name = name ?? "Amigo",
            AcceptedTermsVersion = version,
            Tin = tin,
            AllowCPRLookup = true
        };
        var descriptor = new UserDescriptor(null!)
        {
            Id = user.Id,
            ProviderId = user.ProviderId,
            Name = user.Name,
            AcceptedTermsVersion = user.AcceptedTermsVersion,
            Tin = user.Tin,
            AllowCPRLookup = user.AllowCPRLookup,
            EncryptedAccessToken = accesToken ?? "",
            EncryptedIdentityToken = identityToken ?? ""
        };
        if (addToMock)
        {
            Mock.Get(service)
                .Setup(it => it.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(value: user);
        }
        return descriptor;
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
