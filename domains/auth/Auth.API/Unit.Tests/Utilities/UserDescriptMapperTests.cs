using API.Models.Entities;
using API.Utilities;
using AuthLibrary.Options;
using AuthLibrary.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tests.Utilities;

public class UserDescriptMapperTests
{
    private readonly IUserDescriptMapper mapper;
    private readonly ICryptography cryptography;
    private readonly ILogger<UserDescriptMapper> logger = Mock.Of<ILogger<UserDescriptMapper>>();

    public UserDescriptMapperTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        var options = configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!;

        cryptography = new Cryptography(Microsoft.Extensions.Options.Options.Create(options));

        mapper = new UserDescriptMapper(cryptography, logger);
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
}
