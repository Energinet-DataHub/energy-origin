using EnergyOrigin.TokenValidation.Utilities;
using System.Security.Cryptography;
using System.Text;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Utilities;

public class RsaKeyGeneratorTests
{
    [Fact]
    public void GenerateTestKey_ShouldGenerateValidPrivateKey()
    {
        var keyAsString = Encoding.UTF8.GetString(RsaKeyGenerator.GenerateTestKey());

        Assert.NotNull(keyAsString);
        Assert.StartsWith("-----BEGIN RSA PRIVATE KEY-----", keyAsString);
        Assert.EndsWith("-----END RSA PRIVATE KEY-----", keyAsString.Trim());
    }

    [Fact]
    public void GenerateTestKey_ShouldGenerateUniqueKeyEachTime()
    {
        var key1 = RsaKeyGenerator.GenerateTestKey();
        var key2 = RsaKeyGenerator.GenerateTestKey();

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateTestKey_ShouldGenerateKeyWithValidFormat()
    {
        var keyAsString = Encoding.UTF8.GetString(RsaKeyGenerator.GenerateTestKey());

        var rsa = RSA.Create();
        var exception = Record.Exception(() => rsa.ImportFromPem(keyAsString))!;

        Assert.Null(exception);
    }
}
