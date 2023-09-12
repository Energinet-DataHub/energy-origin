using API.Models.Entities;
using API.Utilities;
using API.Values;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Configuration;

namespace Unit.Tests.Utilities;

public class UserExtensionsTests
{
    private ICryptography cryptography;

    public UserExtensionsTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        var options = configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!;

        cryptography = new Cryptography(options);
    }

    [Fact]
    public void MapDescriptor_ShouldReturnDescriptorWithProperties_WhenMappingDatabaseUserWithTokens()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "TestUser", AllowCprLookup = true, UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 3 } } };

        var accesToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var providerType = ProviderType.NemIdProfessional;

        var descriptor = user.MapDescriptor(cryptography, providerType, Array.Empty<string>(), accesToken, identityToken);

        Assert.NotNull(descriptor);
        Assert.Equal(user.Id, descriptor.Id);
        Assert.Equal(user.Name, descriptor.Name);
        Assert.Equal(user.AllowCprLookup, descriptor.AllowCprLookup);
        Assert.Equal(accesToken, cryptography.Decrypt<string>(descriptor.EncryptedAccessToken));
        Assert.NotEqual(accesToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, cryptography.Decrypt<string>(descriptor.EncryptedIdentityToken));
        Assert.Equal(providerType, descriptor.ProviderType);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
    }
}
