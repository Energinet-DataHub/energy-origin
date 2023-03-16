//using API.Options;
//using API.Utilities;
//using AuthLibrary.Utilities;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Options;

//namespace Tests.Utilities;

//public class CryptographyTests
//{
//    private readonly ICryptography cryptography;

//    public CryptographyTests()
//    {
//        var configuration = new ConfigurationBuilder()
//            .SetBasePath(Directory.GetCurrentDirectory())
//            .AddJsonFile("appsettings.Test.json", false)
//            .Build();

//        var options = configuration.GetSection(CryptographyOptions.Prefix).Get<CryptographyOptions>()!;

//        cryptography = new Cryptography(Options.Create(options));
//    }

//    [Fact]
//    public void Encrypt_ShouldReturnAString_WhenEncryptingAString()
//    {
//        var value = Guid.NewGuid().ToString();

//        var encrypted = cryptography.Encrypt(value);

//        Assert.NotNull(encrypted);
//        Assert.IsType<string>(encrypted);
//    }

//    [Fact]
//    public void Encrypt_ShouldReturnAString_WhenEncryptingAnObject()
//    {
//        var value = new TestRecord(Guid.NewGuid().ToString(), Random.Shared.Next());

//        var encrypted = cryptography.Encrypt(value);

//        Assert.NotNull(encrypted);
//        Assert.IsType<string>(encrypted);
//    }

//    [Fact]
//    public void Decrypt_ShouldRestoreOriginalValue_WhenDecryptingAString()
//    {
//        var value = Guid.NewGuid().ToString();

//        var decrypted = cryptography.Decrypt<string>(cryptography.Encrypt(value));

//        Assert.NotNull(decrypted);
//        Assert.IsType<string>(decrypted);
//        Assert.Equal(value, decrypted);
//    }

//    [Fact]
//    public void Decrypt_ShouldRestoreOriginalValue_WhenDecryptingAnObject()
//    {
//        var value = new TestRecord(Guid.NewGuid().ToString(), Random.Shared.Next());

//        var decrypted = cryptography.Decrypt<TestRecord>(cryptography.Encrypt(value));

//        Assert.NotNull(decrypted);
//        Assert.IsType<TestRecord>(decrypted);
//        Assert.Equal(value, decrypted);
//    }

//    private record TestRecord(string valueA, int valueB);
//}
