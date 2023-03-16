using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Options;
using API.Utilities;
using API.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tests.Utilities;

public class UserDescriptMapperTests
{
    private readonly IClaimsWrapperMapper mapper;
    private readonly ICryptography cryptography;
    private readonly ILogger<ClaimsWrapperMapper> logger = Mock.Of<ILogger<ClaimsWrapperMapper>>();

    public UserDescriptMapperTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        var options = configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!;

        cryptography = new Cryptography(Options.Create(options));

        mapper = new ClaimsWrapperMapper(cryptography, logger);
    }

    [Fact]
    public void Map_ShouldReturnClaimsWrapperWithProperties_WhenMappingDatabaseUserWithTokens()
    {
        var user = new User()
        {
            Id = Guid.NewGuid(),
            ProviderId = Guid.NewGuid().ToString(),
            Name = "Amigo",
            AcceptedTermsVersion = 0,
            AllowCPRLookup = true
        };
        var accesToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();

        var claimsWrapper = mapper.Map(user, accesToken, identityToken);

        Assert.NotNull(claimsWrapper);
        Assert.Equal(user.Id, claimsWrapper.Id);
        Assert.Equal(user.ProviderId, claimsWrapper.ProviderId);
        Assert.Equal(user.Name, claimsWrapper.Name);
        Assert.Equal(user.AcceptedTermsVersion, claimsWrapper.AcceptedTermsVersion);
        Assert.Equal(user.AllowCPRLookup, claimsWrapper.AllowCPRLookup);
        Assert.Equal(accesToken, claimsWrapper.AccessToken);
        Assert.NotEqual(accesToken, claimsWrapper.EncryptedAccessToken);
        Assert.Equal(identityToken, claimsWrapper.IdentityToken);
        Assert.NotEqual(identityToken, claimsWrapper.EncryptedIdentityToken);
    }

    [Fact]
    public void Map_ShouldReturnClaimsWrapperWithProperties_WhenMappingClaimPrincipal()
    {
        var id = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var scope = $"{Guid.NewGuid()} {Guid.NewGuid()}";
        var accessToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid().ToString();
        var version = Random.Shared.Next();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id),
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim(UserClaimName.Scope, scope),
            new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
            new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
            new Claim(UserClaimName.ProviderId, providerId),
            new Claim(UserClaimName.TermsVersion, $"{version}"),
            new Claim(UserClaimName.AllowCPRLookup, "true"),
        }, "mock"));

        var claimsWrapper = mapper.Map(user);

        Assert.NotNull(claimsWrapper);
        Assert.Equal(id, claimsWrapper.Id?.ToString());
        Assert.Equal(providerId, claimsWrapper.ProviderId);
        Assert.Equal(name, claimsWrapper.Name);
        Assert.Equal(version, claimsWrapper.AcceptedTermsVersion);
        Assert.Null(claimsWrapper.Tin);
        Assert.True(claimsWrapper.AllowCPRLookup);
        Assert.Equal(accessToken, claimsWrapper.AccessToken);
        Assert.NotEqual(accessToken, claimsWrapper.EncryptedAccessToken);
        Assert.Equal(identityToken, claimsWrapper.IdentityToken);
        Assert.NotEqual(identityToken, claimsWrapper.EncryptedIdentityToken);
    }

    [Fact]
    public void Map_ShouldReturnClaimsWrapperWithProperties_WhenMappingClaimPrincipalWithoutId()
    {
        var name = Guid.NewGuid().ToString();
        var scope = $"{Guid.NewGuid()} {Guid.NewGuid()}";
        var accessToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid().ToString();
        var version = Random.Shared.Next();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim(UserClaimName.Scope, scope),
            new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
            new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
            new Claim(UserClaimName.ProviderId, providerId),
            new Claim(UserClaimName.TermsVersion, $"{version}"),
            new Claim(UserClaimName.AllowCPRLookup, "true"),
        }, "mock"));

        var claimsWrapper = mapper.Map(user);

        Assert.NotNull(claimsWrapper);
        Assert.Equal(providerId, claimsWrapper.ProviderId);
        Assert.Equal(name, claimsWrapper.Name);
        Assert.Equal(version, claimsWrapper.AcceptedTermsVersion);
        Assert.Null(claimsWrapper.Tin);
        Assert.True(claimsWrapper.AllowCPRLookup);
        Assert.Equal(accessToken, claimsWrapper.AccessToken);
        Assert.NotEqual(accessToken, claimsWrapper.EncryptedAccessToken);
        Assert.Equal(identityToken, claimsWrapper.IdentityToken);
        Assert.NotEqual(identityToken, claimsWrapper.EncryptedIdentityToken);
    }

    [Fact]
    public void Map_ShouldReturnNull_WhenMappingANullClaimPrincipal()
    {
        var claimsWrapper = mapper.Map(null);

        Assert.Null(claimsWrapper);
    }

    [Fact]
    public void Map_ShouldReturnNull_WhenMappingClaimPrincipalWithoutRequiredProperties()
    {
        var name = Guid.NewGuid().ToString();
        var accessToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid().ToString();
        var version = Random.Shared.Next();

        var cases = new Dictionary<string, Claim[]>
        {
            { JwtRegisteredClaimNames.Name, new Claim[] {
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.ProviderId, providerId),
                new Claim(UserClaimName.TermsVersion, $"{version}"),
                new Claim(UserClaimName.AllowCPRLookup, "true"),
            }},
            { UserClaimName.AccessToken, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.ProviderId, providerId),
                new Claim(UserClaimName.TermsVersion, $"{version}"),
                new Claim(UserClaimName.AllowCPRLookup, "true"),
            }},
            { UserClaimName.IdentityToken, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.ProviderId, providerId),
                new Claim(UserClaimName.TermsVersion, $"{version}"),
                new Claim(UserClaimName.AllowCPRLookup, "true"),
            }},
            { UserClaimName.ProviderId, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.TermsVersion, $"{version}"),
                new Claim(UserClaimName.AllowCPRLookup, "true"),
            }},
                { UserClaimName.TermsVersion, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.ProviderId, providerId),
                new Claim(UserClaimName.AllowCPRLookup, "true"),
            }},
                { UserClaimName.AllowCPRLookup, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.ProviderId, providerId),
                new Claim(UserClaimName.TermsVersion, $"{version}"),
            }},
        };

        foreach (var kase in cases)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(kase.Value, "mock"));

            var claimsWrapper = mapper.Map(user);

            Assert.True(claimsWrapper == null, $"ClaimsWrapper was made without required property: '{kase.Key}'");
        }
    }

    [Fact]
    public void Map_ShouldLogMessage_WhenFailing()
    {
        var claimsWrapper = mapper.Map(null);

        Assert.Null(claimsWrapper);

        Mock.Get(logger).Verify(it => it.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}
