using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Logging;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Utilities;

public class UserDescriptMapperBaseTests
{
    private readonly IUserDescriptorMapperBase mapper;
    private readonly ICryptography cryptography;
    private readonly ILogger<UserDescriptorMapperBase> logger = Mock.Of<ILogger<UserDescriptorMapperBase>>();

    public UserDescriptMapperBaseTests()
    {
        var options = new CryptographyOptions()
        {
            Key = "secretsecretsecretsecret"
        };

        cryptography = new Cryptography(options);
        mapper = new UserDescriptorMapperBase(cryptography, logger);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithProperties_WhenMappingClaimPrincipal()
    {
        var id = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var scope = $"{Guid.NewGuid()} {Guid.NewGuid()}";
        var accessToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var matchedRoles = Guid.NewGuid().ToString();
        var providerType = ProviderType.MitIdPrivate;
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var providerKey = Guid.NewGuid().ToString();
        var providerKeys = $"{providerKeyType}={providerKey}";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim(UserClaimName.Actor, id),
            new Claim(UserClaimName.Scope, scope),
            new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
            new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
            new Claim(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
            new Claim(UserClaimName.ProviderType, providerType.ToString()),
            new Claim(UserClaimName.MatchedRoles, matchedRoles),
            new Claim(UserClaimName.AllowCprLookup, "true"),
        }, "mock"));

        var descriptor = mapper.Map(user);

        Assert.NotNull(descriptor);
        Assert.Equal(id, descriptor.Id!.ToString());
        Assert.Equal(providerType, descriptor.ProviderType);
        Assert.Equal(name, descriptor.Name);
        Assert.Null(descriptor.Tin);
        Assert.True(descriptor.AllowCprLookup);
        Assert.Equal(accessToken, descriptor.AccessToken);
        Assert.NotEqual(accessToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, descriptor.IdentityToken);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
        Assert.NotEqual(providerKeys, descriptor.EncryptedProviderKeys);
        Assert.Equal(matchedRoles, descriptor.MatchedRoles);
        Assert.Single(descriptor.ProviderKeys);
        Assert.Equal(descriptor.ProviderKeys.Single().Key, providerKeyType);
        Assert.Equal(descriptor.ProviderKeys.Single().Value, providerKey);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithProperties_WhenMappingClaimPrincipalWithoutId()
    {
        var id = Guid.NewGuid().ToString();
        var name = Guid.NewGuid().ToString();
        var scope = $"{Guid.NewGuid()} {Guid.NewGuid()}";
        var accessToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var matchedRoles = Guid.NewGuid().ToString();
        var providerType = ProviderType.MitIdPrivate;
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var providerKey = Guid.NewGuid().ToString();
        var providerKeys = $"{providerKeyType}={providerKey}";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim(UserClaimName.Scope, scope),
            new Claim(UserClaimName.Actor, id),
            new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
            new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
            new Claim(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
            new Claim(UserClaimName.ProviderType, providerType.ToString()),
            new Claim(UserClaimName.MatchedRoles, matchedRoles),
            new Claim(UserClaimName.AllowCprLookup, "true"),
        }, "mock"));

        var descriptor = mapper.Map(user);

        Assert.NotNull(descriptor);
        Assert.Equal(providerType, descriptor.ProviderType);
        Assert.Equal(name, descriptor.Name);
        Assert.Null(descriptor.Tin);
        Assert.True(descriptor.AllowCprLookup);
        Assert.Equal(accessToken, descriptor.AccessToken);
        Assert.NotEqual(accessToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, descriptor.IdentityToken);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
        Assert.Equal(matchedRoles, descriptor.MatchedRoles);
        Assert.Single(descriptor.ProviderKeys);
        Assert.Equal(descriptor.ProviderKeys.Single().Key, providerKeyType);
        Assert.Equal(descriptor.ProviderKeys.Single().Value, providerKey);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithMatchedRoleSetToEmptyString_WhenMappingClaimPrincipalWithoutMatchedRoles()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(JwtRegisteredClaimNames.Name, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.Scope, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.Actor, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.AccessToken, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.IdentityToken, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.ProviderKeys, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.ProviderType, ProviderType.MitIdPrivate.ToString()),
            new Claim(UserClaimName.AllowCprLookup, "false"),
        }, "mock"));

        var descriptor = mapper.Map(user);

        Assert.NotNull(descriptor);
        Assert.Equal(string.Empty, descriptor.MatchedRoles);
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
        var providerType = ProviderType.MitIdPrivate;
        var providerKeyType = ProviderKeyType.MitIdUuid;
        var providerKey = Guid.NewGuid().ToString();
        var providerKeys = $"{providerKeyType}={providerKey}";

        var cases = new Dictionary<string, Claim[]>
        {
            { JwtRegisteredClaimNames.Name, new Claim[] {
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.ProviderType, providerType.ToString()),
                new Claim(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
                new Claim(UserClaimName.AllowCprLookup, "true"),
            }},
            { UserClaimName.AccessToken, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.ProviderType, providerType.ToString()),
                new Claim(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
                new Claim(UserClaimName.AllowCprLookup, "true"),
            }},
            { UserClaimName.IdentityToken, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.ProviderType, providerType.ToString()),
                new Claim(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
                new Claim(UserClaimName.AllowCprLookup, "true"),
            }},
            { UserClaimName.ProviderType, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.AllowCprLookup, "true"),
            }},
                { UserClaimName.AllowCprLookup, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.ProviderType, providerType.ToString()),
                new Claim(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
            }},
                { UserClaimName.ProviderKeys, new Claim[] {
                new Claim(JwtRegisteredClaimNames.Name, name),
                new Claim(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new Claim(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new Claim(UserClaimName.ProviderType, providerType.ToString()),
                new Claim(UserClaimName.AllowCprLookup, "true"),
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

    [Fact]
    public void Map_ShouldThrowException_WhenActorFormatWrong()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(JwtRegisteredClaimNames.Name, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.Actor, "random-string"),
            new Claim(UserClaimName.Scope, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.AccessToken, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.IdentityToken, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.ProviderKeys, Guid.NewGuid().ToString()),
            new Claim(UserClaimName.ProviderType, ProviderType.MitIdPrivate.ToString()),
            new Claim(UserClaimName.AllowCprLookup, "true"),
        }, "mock"));

        Assert.Throws<FormatException>(() => mapper.Map(user));
    }
}
