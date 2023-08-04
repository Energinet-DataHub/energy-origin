using API.Models.Entities;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Utilities;

public class UserDescriptorMapperTests
{
    private readonly IUserDescriptorMapper mapper;
    private readonly ILogger<UserDescriptorMapper> logger = Mock.Of<ILogger<UserDescriptorMapper>>();

    public UserDescriptorMapperTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        var options = configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!;

        ICryptography cryptography = new Cryptography(Moptions.Create(options));

        mapper = new UserDescriptorMapper(cryptography, logger);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithProperties_WhenMappingDatabaseUserWithTokens()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "TestUser", AllowCprLookup = true, UserTerms = new List<UserTerms> { new() { Type = UserTermsType.PrivacyPolicy, AcceptedVersion = 3 } } };

        var accesToken = Guid.NewGuid().ToString();
        var identityToken = Guid.NewGuid().ToString();
        var providerType = ProviderType.NemIdProfessional;

        var descriptor = mapper.Map(user, providerType, accesToken, identityToken);

        Assert.NotNull(descriptor);
        Assert.Equal(user.Id, descriptor.Id);
        Assert.Equal(user.Name, descriptor.Name);
        Assert.Contains(user.UserTerms, x => x.AcceptedVersion == descriptor.AcceptedPrivacyPolicyVersion);
        Assert.Equal(user.AllowCprLookup, descriptor.AllowCprLookup);
        Assert.Equal(accesToken, descriptor.AccessToken);
        Assert.NotEqual(accesToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, descriptor.IdentityToken);
        Assert.Equal(providerType, descriptor.ProviderType);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
    }
}
