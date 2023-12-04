using EnergyOrigin.TokenValidation.Utilities;
using System.Security.Cryptography;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Utilities;

public class RsaKeyGeneratorTests
{
    [Fact]
    public void GenerateTestKey_ShouldGenerateValidPrivateKey()
    {
        var key = RsaKeyGenerator.GenerateTestKey();

        Assert.NotNull(key);
        Assert.StartsWith("-----BEGIN RSA PRIVATE KEY-----", key);
        Assert.EndsWith("-----END RSA PRIVATE KEY-----", key.Trim());
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
        var key = RsaKeyGenerator.GenerateTestKey();

        var rsa = RSA.Create();
        var exception = Record.Exception(() => rsa.ImportFromPem(key))!;

        Assert.Null(exception);
    }
}
