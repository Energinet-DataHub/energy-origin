using API.Models.Entities;
using API.Utilities;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tests.Utilities;

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

        cryptography = new Cryptography(Options.Create(options));

        mapper = new UserDescriptorMapper(cryptography, logger);
    }

    [Fact]
    public void Map_ShouldReturnDescriptorWithProperties_WhenMappingDatabaseUserWithTokens()
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

        var descriptor = mapper.Map(user, accesToken, identityToken);

        Assert.NotNull(descriptor);
        Assert.Equal(user.Id, descriptor.Id);
        Assert.Equal(user.ProviderId, descriptor.ProviderId);
        Assert.Equal(user.Name, descriptor.Name);
        Assert.Equal(user.AcceptedTermsVersion, descriptor.AcceptedTermsVersion);
        Assert.Equal(user.AllowCPRLookup, descriptor.AllowCPRLookup);
        Assert.Equal(accesToken, descriptor.AccessToken);
        Assert.NotEqual(accesToken, descriptor.EncryptedAccessToken);
        Assert.Equal(identityToken, descriptor.IdentityToken);
        Assert.NotEqual(identityToken, descriptor.EncryptedIdentityToken);
    }
}
