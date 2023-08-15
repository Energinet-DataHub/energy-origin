using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using static API.Utilities.TokenIssuer;

namespace Unit.Tests.Utilities;

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

    [Theory]
    [InlineData(UserScopeClaim.NotAcceptedPrivacyPolicy + " " + UserScopeClaim.NotAcceptedTermsOfService, 0, 0, false)]
    [InlineData($"{UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}", 3, 1, false)]
    [InlineData($"{UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}", 0, 0, true)]
    [InlineData($"{UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}", 3, 1, true)]
    public void Issue_ShouldReturnTokenForUserWithCorrectScope_WhenInvokedWithDifferentVersionsAndBypassValues(string expectedScope, int privacyVersion, int tosVersion, bool bypass)
    {
        var (descriptor, data) = PrepareUser(privacyVersion: privacyVersion, tosVersion: tosVersion);

        var token = GetTokenIssuer().Issue(descriptor, data, versionBypass: bypass);

        var scope = Convert(token)!.Claims.First(x => x.Type == UserClaimName.Scope)!.Value;
        Assert.Equal(expectedScope, scope);
    }

    [Fact]
    public void Issue_ShouldReturnATokenForThatUser_WhenIssuingForAUser()
    {
        var (descriptor, data) = PrepareUser();

        var token = GetTokenIssuer().Issue(descriptor, data);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(descriptor.Id.ToString(), jwt.Subject);
    }

    [Fact]
    public void Issue_ShouldReturnATokenWithCorrectValidityTimes_WhenIssuingAtASpecifiedTime()
    {
        var (descriptor, data) = PrepareUser();
        var duration = new TimeSpan(10, 11, 12);
        var options = TestOptions.Token(tokenOptions, duration: duration);
        var issueAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var token = GetTokenIssuer(token: options).Issue(descriptor, data, issueAt: issueAt);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issueAt, jwt.ValidFrom);
        Assert.Equal(issueAt.Add(duration), jwt.ValidTo);
    }

    [Fact]
    public void Issue_ShouldReturnATokenCreatedUsingOptions_WhenIssuing()
    {
        var (descriptor, data) = PrepareUser();
        var audience = Guid.NewGuid().ToString();
        var issuer = Guid.NewGuid().ToString();
        var options = TestOptions.Token(tokenOptions, audience, issuer);

        var token = GetTokenIssuer(token: options).Issue(descriptor, data);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(issuer, jwt.Issuer);
        Assert.Contains(audience, jwt.Audiences);
    }

    [Fact]
    public void Issue_ShouldReturnASignedToken_WhenIssuing()
    {
        var (descriptor, data) = PrepareUser();
        var options = TestOptions.Token(tokenOptions);

        var token = GetTokenIssuer(token: options).Issue(descriptor, data);

        var rsa = RSA.Create();
        rsa.ImportFromPem(Encoding.UTF8.GetString(options.PublicKeyPem));
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
        var (descriptor, data) = PrepareUser(name: name, privacyVersion: version, accessToken: accessToken, identityToken: identityToken);

        var token = GetTokenIssuer().Issue(descriptor, data);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.Equal(descriptor.Id.ToString(), jwt.Claims.SingleOrDefault(it => it.Type == JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(name, jwt.Claims.SingleOrDefault(it => it.Type == JwtRegisteredClaimNames.Name)?.Value);
        Assert.Equal(descriptor.AllowCprLookup, jwt.Claims.SingleOrDefault(it => it.Type == UserClaimName.AllowCprLookup)?.Value == "true");
        Assert.Equal(!descriptor.AllowCprLookup, jwt.Claims.SingleOrDefault(it => it.Type == UserClaimName.AllowCprLookup)?.Value == "false");
        Assert.Equal(accessToken, jwt.Claims.SingleOrDefault(it => it.Type == UserClaimName.AccessToken)?.Value);
        Assert.Equal(identityToken, jwt.Claims.SingleOrDefault(it => it.Type == UserClaimName.IdentityToken)?.Value);
    }

    [Fact]
    public void Issue_ShouldReturnAToken_WhenIssuingForAnUnsavedUser()
    {
        var (descriptor, data) = PrepareUser(addToMock: false);

        var token = GetTokenIssuer().Issue(descriptor, data);

        var jwt = Convert(token);
        Assert.NotNull(jwt);
        Assert.NotNull(jwt.Claims.SingleOrDefault(it => it.Type == JwtRegisteredClaimNames.Sub));
        // FIXME: this
        // Assert.Equal("false", jwt.Claims.SingleOrDefault(it => it.Type == UserClaimName.UserStored)?.Value);
    }

    private TokenIssuer GetTokenIssuer(TermsOptions? terms = default, TokenOptions? token = default) => new(terms ?? termsOptions, token ?? tokenOptions);

    private (UserDescriptor, UserData) PrepareUser(string? name = default, int privacyVersion = 3, int tosVersion = 1, string? accessToken = default, string? identityToken = default, bool addToMock = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Name = name ?? "Amigo", AllowCprLookup = false,
            Company = new Company
            {
                Name = "testCompany"
            }
        };
        var descriptor = new UserDescriptor(null!)
        {
            Id = user.Id.Value,
            Name = user.Name,
            AllowCprLookup = user.AllowCprLookup,
            ProviderType = ProviderType.NemIdProfessional,
            EncryptedAccessToken = accessToken ?? "",
            EncryptedIdentityToken = identityToken ?? "",
            MatchedRoles = ""
        };
        if (addToMock)
        {
            Mock.Get(service)
                .Setup(it => it.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(value: user);
        }
        return (descriptor, new UserData(privacyVersion, tosVersion));
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
