using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Utilities;

public class UserDescriptorTests
{
    private readonly ICryptography cryptography;

    public UserDescriptorTests()
    {
        var options = new CryptographyOptions()
        {
            Key = "secretsecretsecretsecret"
        };

        cryptography = new Cryptography(options);
    }

    [Theory]
    [InlineData("dd76accc-e477-4895-9e04-763309acee4e", "8d025d7a-eae2-4242-a861-43bc4e6a2c16", "8d025d7a-eae2-4242-a861-43bc4e6a2c16")]
    [InlineData("dd76accc-e477-4895-9e04-763309acee4e", null, "dd76accc-e477-4895-9e04-763309acee4e")]
    public void UserDescriptor_ShouldCalculateSubject_WhenSuppliedWithUserAndCompanyIds(string userId, string? organizationId, string expectedSubject)
    {
        var organization = Guid.TryParse(organizationId, out var organizationGuid) ? new OrganizationDescriptor { Id = organizationGuid, Tin = "", Name = "" } : null;
        var descriptor = Make(id: Guid.Parse(userId), organization: organization);

        Assert.Equal(expectedSubject, descriptor.Subject.ToString());
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
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, name),
            new(UserClaimName.Actor, id),
            new(UserClaimName.Scope, scope),
            new(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
            new(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
            new(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
            new(UserClaimName.ProviderType, providerType.ToString()),
            new(UserClaimName.MatchedRoles, matchedRoles),
            new(UserClaimName.AllowCprLookup, "true"),
        }, "mock"));

        var descriptor = new UserDescriptor(user);

        Assert.NotNull(descriptor);
        Assert.Equal(id, descriptor.Id!.ToString());
        Assert.Equal(providerType, descriptor.ProviderType);
        Assert.Equal(name, descriptor.Name);
        Assert.Null(descriptor.Organization?.Tin);
        Assert.True(descriptor.AllowCprLookup);
        Assert.NotEqual(accessToken, descriptor.EncryptedAccessToken);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
        Assert.NotEqual(providerKeys, descriptor.EncryptedProviderKeys);
        Assert.Equal(matchedRoles, descriptor.MatchedRoles);
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
            new(JwtRegisteredClaimNames.Name, name),
            new(UserClaimName.Scope, scope),
            new(UserClaimName.Actor, id),
            new(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
            new(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
            new(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
            new(UserClaimName.ProviderType, providerType.ToString()),
            new(UserClaimName.MatchedRoles, matchedRoles),
            new(UserClaimName.AllowCprLookup, "true"),
        }, "mock"));

        var descriptor = new UserDescriptor(user);

        Assert.NotNull(descriptor);
        Assert.Equal(providerType, descriptor.ProviderType);
        Assert.Equal(name, descriptor.Name);
        Assert.Null(descriptor.Organization?.Tin);
        Assert.True(descriptor.AllowCprLookup);
        Assert.NotEqual(accessToken, descriptor.EncryptedAccessToken);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
        Assert.NotEqual(providerKeys, descriptor.EncryptedProviderKeys);
        Assert.Equal(matchedRoles, descriptor.MatchedRoles);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithMatchedRoleSetToEmptyString_WhenMappingClaimPrincipalWithoutMatchedRoles()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new(JwtRegisteredClaimNames.Name, Guid.NewGuid().ToString()),
            new(UserClaimName.Scope, Guid.NewGuid().ToString()),
            new(UserClaimName.Actor, Guid.NewGuid().ToString()),
            new(UserClaimName.AccessToken, Guid.NewGuid().ToString()),
            new(UserClaimName.IdentityToken, Guid.NewGuid().ToString()),
            new(UserClaimName.ProviderKeys, Guid.NewGuid().ToString()),
            new(UserClaimName.ProviderType, ProviderType.MitIdPrivate.ToString()),
            new(UserClaimName.AllowCprLookup, "false"),
        }, "mock"));

        var descriptor = new UserDescriptor(user);

        Assert.NotNull(descriptor);
        Assert.Equal(string.Empty, descriptor.MatchedRoles);
    }

    [Fact]
    public void Map_ShouldReturnNull_WhenMappingANullClaimPrincipal() => Assert.Throws<PropertyMissingException>(() => new UserDescriptor((ClaimsPrincipal)null!));

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
                new(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new(UserClaimName.ProviderType, providerType.ToString()),
                new(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
                new(UserClaimName.AllowCprLookup, "true"),
            }},
            { UserClaimName.AccessToken, new Claim[] {
                new(JwtRegisteredClaimNames.Name, name),
                new(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new(UserClaimName.ProviderType, providerType.ToString()),
                new(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
                new(UserClaimName.AllowCprLookup, "true"),
            }},
            { UserClaimName.IdentityToken, new Claim[] {
                new(JwtRegisteredClaimNames.Name, name),
                new(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new(UserClaimName.ProviderType, providerType.ToString()),
                new(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
                new(UserClaimName.AllowCprLookup, "true"),
            }},
            { UserClaimName.ProviderType, new Claim[] {
                new(JwtRegisteredClaimNames.Name, name),
                new(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
                new(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new(UserClaimName.AllowCprLookup, "true"),
            }},
                { UserClaimName.AllowCprLookup, new Claim[] {
                new(JwtRegisteredClaimNames.Name, name),
                new(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new(UserClaimName.ProviderType, providerType.ToString()),
                new(UserClaimName.ProviderKeys, cryptography.Encrypt(providerKeys)),
            }},
                { UserClaimName.ProviderKeys, new Claim[] {
                new(JwtRegisteredClaimNames.Name, name),
                new(UserClaimName.AccessToken, cryptography.Encrypt(accessToken)),
                new(UserClaimName.IdentityToken, cryptography.Encrypt(identityToken)),
                new(UserClaimName.ProviderType, providerType.ToString()),
                new(UserClaimName.AllowCprLookup, "true"),
            }},
        };

        foreach (var kase in cases)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(kase.Value, "mock"));

            try
            {
                var descriptor = new UserDescriptor(user);
                Assert.Fail($"Descriptor was made without required property: '{kase.Key}'");
            }
            catch { }
        }
    }

    [Fact]
    public void Map_ShouldThrowException_WhenActorFormatWrong()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new(JwtRegisteredClaimNames.Name, Guid.NewGuid().ToString()),
            new(UserClaimName.Actor, "random-string"),
            new(UserClaimName.Scope, Guid.NewGuid().ToString()),
            new(UserClaimName.AccessToken, Guid.NewGuid().ToString()),
            new(UserClaimName.IdentityToken, Guid.NewGuid().ToString()),
            new(UserClaimName.ProviderKeys, Guid.NewGuid().ToString()),
            new(UserClaimName.ProviderType, ProviderType.MitIdPrivate.ToString()),
            new(UserClaimName.AllowCprLookup, "true"),
        }, "mock"));

        Assert.Throws<FormatException>(() => new UserDescriptor(user));
    }

    private UserDescriptor Make(
        Guid? id = default,
        string? name = default,
        ProviderType providerType = ProviderType.MitIdPrivate,
        OrganizationDescriptor? organization = default,
        string? scope = default,
        string? allowCprLookup = default,
        string? matchedRoles = default,
        string? encryptedAccessToken = default,
        string? encryptedIdentityToken = default,
        string? encryptedProviderKeys = default
    )
    {
        var identity = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, name ?? ""),
            new(JwtRegisteredClaimNames.Sub, id?.ToString() ?? Guid.NewGuid().ToString()),
            new(UserClaimName.ProviderType, providerType.ToString()),
            new(UserClaimName.AllowCprLookup, allowCprLookup ?? "false"),
            new(UserClaimName.Actor, id?.ToString() ?? Guid.NewGuid().ToString()),
            new(UserClaimName.MatchedRoles, matchedRoles ?? ""),
            new(UserClaimName.Scope, scope ?? ""),
            new(UserClaimName.AccessToken, encryptedAccessToken ?? ""),
            new(UserClaimName.IdentityToken, encryptedIdentityToken ?? ""),
            new(UserClaimName.ProviderKeys, encryptedProviderKeys ?? ""),
        };

        if (organization != null)
        {
            identity.Add(new(UserClaimName.OrganizationId, organization.Id.ToString()));
            identity.Add(new(UserClaimName.OrganizationName, organization.Name));
            identity.Add(new(UserClaimName.Tin, organization.Tin));
        }

        return new UserDescriptor(new ClaimsPrincipal(new ClaimsIdentity(identity)));
    }
}
