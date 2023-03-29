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

        cryptography = new Cryptography(Microsoft.Extensions.Options.Options.Create(options));
    }

    [Theory]
    [InlineData("dd76accc-e477-4895-9e04-763309acee4e", "8d025d7a-eae2-4242-a861-43bc4e6a2c16", "8d025d7a-eae2-4242-a861-43bc4e6a2c16")]
    [InlineData("dd76accc-e477-4895-9e04-763309acee4e", null, "dd76accc-e477-4895-9e04-763309acee4e")]
    public void UserDescriptor_ShouldCalculateSubject_WhenSuppliedWithUserAndCompanyIds(string userId, string? companyId, string expectedSubject)
    {
        var descriptor = new UserDescriptor(null!)
        {
            Id = Guid.Parse(userId),
            CompanyId = Guid.TryParse(companyId, out var companyGuid) ? companyGuid : null,
        };

        Assert.Equal(expectedSubject, descriptor.Subject.ToString());
    }

    [Fact]
    public void UserDescriptor_ShouldUnencryptAndFormatKeys_WhenProviderKeysIsReferenced()
    {
        var dict = new Dictionary<ProviderKeyType, string>
        {
            { ProviderKeyType.PID, Guid.NewGuid().ToString() },
            { ProviderKeyType.RID, Guid.NewGuid().ToString() },
            { ProviderKeyType.MitID_UUID, Guid.NewGuid().ToString() },
        };

        var providerKeys = string.Join(" ", dict.Select(x => $"{x.Key}={x.Value}"));
        var providerKeysEncrypted = cryptography.Encrypt(providerKeys);

        var descriptor = new UserDescriptor(cryptography)
        {
            EncryptedProviderKeys = providerKeysEncrypted
        };

        var keys = descriptor.ProviderKeys;

        Assert.Equal(3, dict.Count);
        Assert.Equal(keys.ElementAt(0).Key, dict.ElementAt(0).Key);
        Assert.Equal(keys.ElementAt(0).Value, dict.ElementAt(0).Value);
        Assert.Equal(keys.ElementAt(1).Key, dict.ElementAt(1).Key);
        Assert.Equal(keys.ElementAt(1).Value, dict.ElementAt(1).Value);
        Assert.Equal(keys.ElementAt(2).Key, dict.ElementAt(2).Key);
        Assert.Equal(keys.ElementAt(2).Value, dict.ElementAt(2).Value);
    }
}
