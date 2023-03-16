using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Models.Entities;
using API.Options;
using API.Utilities;
using API.Values;
using AuthLibrary.Utilities;
using AuthLibrary.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tests.Utilities;

public class UserDescriptMapperTests
{
    private readonly IUserDescriptMapper mapper;
    private readonly ICryptography cryptography;
    private readonly ILogger<UserDescriptMapper> logger = Mock.Of<ILogger<UserDescriptMapper>>();

    //public UserDescriptMapperTests()
    //{
    //    var configuration = new ConfigurationBuilder()
    //        .SetBasePath(Directory.GetCurrentDirectory())
    //        .AddJsonFile("appsettings.Test.json", false)
    //        .Build();

    //    var options = configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!;

    //    cryptography = new Cryptography(Options.Create(options));

    //    mapper = new UserDescriptMapper(cryptography, logger);
    //}

    [Fact]
    public void Map_ShouldReturnDescriptorWithProperties_WhenMappingDatabaseUserWithTokens()
    {
        var user = new User()
        {
            Id = Guid.NewGuid(),
            ProviderId = Guid.NewGuid().ToString(),
            Name = "Amigo",
            AcceptedTermsVersion = 0,
            Tin = null,
            AllowCPRLookup = true
        };
        var accesToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();

        var descriptor = mapper.Map(user, accesToken, identityToken);

        Assert.NotNull(descriptor);
        Assert.Equal(user.Id, descriptor.Id);
        Assert.Equal(user.ProviderId, descriptor.ProviderId);
        Assert.Equal(user.Name, descriptor.Name);
        Assert.Equal(user.AcceptedTermsVersion, descriptor.AcceptedTermsVersion);
        Assert.Equal(user.Tin, descriptor.Tin);
        Assert.Equal(user.AllowCPRLookup, descriptor.AllowCPRLookup);
        Assert.Equal(accesToken, descriptor.AccessToken);
        Assert.NotEqual(accesToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, descriptor.IdentityToken);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithProperties_WhenMappingClaimPrincipal()
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

        var descriptor = mapper.Map(user);

        Assert.NotNull(descriptor);
        Assert.Equal(id, descriptor.Id?.ToString());
        Assert.Equal(providerId, descriptor.ProviderId);
        Assert.Equal(name, descriptor.Name);
        Assert.Equal(version, descriptor.AcceptedTermsVersion);
        Assert.Null(descriptor.Tin);
        Assert.True(descriptor.AllowCPRLookup);
        Assert.Equal(accessToken, descriptor.AccessToken);
        Assert.NotEqual(accessToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, descriptor.IdentityToken);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithProperties_WhenMappingClaimPrincipalWithoutId()
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

        var descriptor = mapper.Map(user);

        Assert.NotNull(descriptor);
        Assert.Equal(providerId, descriptor.ProviderId);
        Assert.Equal(name, descriptor.Name);
        Assert.Equal(version, descriptor.AcceptedTermsVersion);
        Assert.Null(descriptor.Tin);
        Assert.True(descriptor.AllowCPRLookup);
        Assert.Equal(accessToken, descriptor.AccessToken);
        Assert.NotEqual(accessToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, descriptor.IdentityToken);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
    }

    [Fact]
    public void Map_ShouldReturnNull_WhenMappingANullClaimPrincipal()
    {
        var descriptor = mapper.Map(null);

        Assert.Null(descriptor);
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

            var descriptor = mapper.Map(user);

            Assert.True(descriptor == null, $"Descriptor was made without required property: '{kase.Key}'");
        }
    }

    [Fact]
    public void Map_ShouldLogMessage_WhenFailing()
    {
        var descriptor = mapper.Map(null);

        Assert.Null(descriptor);

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
