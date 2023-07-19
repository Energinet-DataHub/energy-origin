using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Unit.Tests.Utilities;

public class TokenIssuerTests
{
    private readonly IUserService service = Mock.Of<IUserService>();

    private readonly IOptions<TermsOptions> termsOptions;
    private readonly IOptions<TokenOptions> tokenOptions;

    public TokenIssuerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        termsOptions = Moptions.Create(configuration.GetSection(TermsOptions.Prefix).Get<TermsOptions>()!);
        tokenOptions = Moptions.Create(configuration.GetSection(TokenOptions.Prefix).Get<TokenOptions>()!);
    }

    [Theory]
    [InlineData(UserScopeClaim.NotAcceptedTerms, 0, false)]
    [InlineData($"{UserScopeClaim.AcceptedTerms} {UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}", 1, false)]
    [InlineData($"{UserScopeClaim.AcceptedTerms} {UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}", 0, true)]
    [InlineData($"{UserScopeClaim.AcceptedTerms} {UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}", 1, true)]
    public void Issue_ShouldReturnTokenForUserWithCorrectScope_WhenInvokedWithDifferentVersionsAndBypassValues(string expectedScope, int version, bool bypass)
    {
        var descriptor = PrepareUser(version: version);

        var token = GetTokenIssuer().Issue(descriptor, versionBypass: bypass);

        var scope = Convert(token)!.Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        Assert.Equal(expectedScope, scope);
    }

    [Fact]
    public void Issue_ShouldReturnATokenForThatUser_WhenIssuingForAUser()
    {
        var descriptor = PrepareUser();

        var token = GetTokenIssuer().Issue(descriptor);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(descriptor.Id.ToString(), jwt.Subject);
    }

    [Fact]
    public void Issue_ShouldReturnATokenWithCorrectValidityTimes_WhenIssuingAtASpecifiedTime()
    {
        var descriptor = PrepareUser();
        var duration = new TimeSpan(10, 11, 12);
        var options = TestOptions.Token(tokenOptions.Value, duration: duration);
        var issueAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var token = GetTokenIssuer(token: options.Value).Issue(descriptor, issueAt: issueAt);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issueAt, jwt.ValidFrom);
        Assert.Equal(issueAt.Add(duration), jwt.ValidTo);
    }

    [Fact]
    public void Issue_ShouldReturnATokenCreatedUsingOptions_WhenIssuing()
    {
        var descriptor = PrepareUser();
        var audience = Guid.NewGuid().ToString();
        var issuer = Guid.NewGuid().ToString();
        var options = TestOptions.Token(tokenOptions.Value, audience, issuer);

        var token = GetTokenIssuer(token: options.Value).Issue(descriptor);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issuer, jwt.Issuer);
        Assert.Contains(audience, jwt.Audiences);
    }

    [Fact]
    public void Issue_ShouldReturnASignedToken_WhenIssuing()
    {
        var descriptor = PrepareUser();
        var options = TestOptions.Token(tokenOptions.Value);

        var token = GetTokenIssuer(token: options.Value).Issue(descriptor);

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
    public void Issue_ShouldReturnATokenWithUsersProperties_WhenIssuingForAUser()
    {
        var name = Guid.NewGuid().ToString();
        var accessToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var version = Random.Shared.Next();
        var descriptor = PrepareUser(name, version, accessToken, identityToken);

        var token = GetTokenIssuer().Issue(descriptor);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(descriptor.Id.ToString(), jwt.Claims.FirstOrDefault(it => it.Type == JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(name, jwt.Claims.FirstOrDefault(it => it.Type == JwtRegisteredClaimNames.Name)?.Value);
        Assert.Equal($"{version}", jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.AcceptedTermsVersion)?.Value);
        Assert.Equal("1", jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.CurrentTermsVersion)?.Value);
        Assert.Equal(descriptor.AllowCprLookup, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.AllowCPRLookup)?.Value == "true");
        Assert.Equal(!descriptor.AllowCprLookup, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.AllowCPRLookup)?.Value == "false");
        Assert.Equal(accessToken, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.AccessToken)?.Value);
        Assert.Equal(identityToken, jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.IdentityToken)?.Value);
    }

    [Fact]
    public void Issue_ShouldReturnAToken_WhenIssuingForAnUnsavedUser()
    {
        var descriptor = PrepareUser(addToMock: false, isStored: false);

        var token = GetTokenIssuer().Issue(descriptor);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.NotNull(jwt.Claims.FirstOrDefault(it => it.Type == JwtRegisteredClaimNames.Sub));
        Assert.Equal("false", jwt.Claims.FirstOrDefault(it => it.Type == UserClaimName.UserStored)?.Value);
    }

    private TokenIssuer GetTokenIssuer(TermsOptions? terms = default, TokenOptions? token = default) => new(Moptions.Create(terms ?? termsOptions.Value), Moptions.Create(token ?? tokenOptions.Value));

    private UserDescriptor PrepareUser(string? name = default, int version = 1, string? accessToken = default, string? identityToken = default, bool addToMock = true, bool isStored = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name ?? "Amigo",
            AcceptedTermsVersion = version,
            AllowCprLookup = true

        };
        var descriptor = new UserDescriptor(null!)
        {
            Id = user.Id.Value,
            Name = user.Name,
            AcceptedTermsVersion = user.AcceptedTermsVersion,
            AllowCprLookup = user.AllowCprLookup,
            ProviderType = ProviderType.NemID_Professional,
            EncryptedAccessToken = accessToken ?? "",
            EncryptedIdentityToken = identityToken ?? "",
            UserStored = isStored
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
