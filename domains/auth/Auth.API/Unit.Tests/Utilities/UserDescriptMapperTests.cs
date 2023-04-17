using API.Models.Entities;
using API.Utilities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Unit.Tests.Utilities;

public class UserDescriptMapperTests
{
    private readonly IUserDescriptorMapper mapper;
    private readonly ICryptography cryptography;
    private readonly ILogger<UserDescriptorMapper> logger = Mock.Of<ILogger<UserDescriptorMapper>>();

    public UserDescriptMapperTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        var options = configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!;

        cryptography = new Cryptography(Moptions.Create(options));

        mapper = new UserDescriptorMapper(cryptography, logger);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithProperties_WhenMappingDatabaseUserWithTokens()
    {
        var user = new User()
        {
            Id = Guid.NewGuid(),
            Name = "Amigo",
            AcceptedTermsVersion = 0,
            AllowCPRLookup = true
        };
        var accesToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var providerType = ProviderType.NemID_Professional;

        var descriptor = mapper.Map(user, providerType, accesToken, identityToken);

        Assert.NotNull(descriptor);
        Assert.Equal(user.Id, descriptor.Id);
        Assert.Equal(user.Name, descriptor.Name);
        Assert.Equal(user.AcceptedTermsVersion, descriptor.AcceptedTermsVersion);
        Assert.Equal(user.AllowCPRLookup, descriptor.AllowCPRLookup);
        Assert.Equal(accesToken, descriptor.AccessToken);
        Assert.NotEqual(accesToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, descriptor.IdentityToken);
        Assert.Equal(providerType, descriptor.ProviderType);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
    }
}
