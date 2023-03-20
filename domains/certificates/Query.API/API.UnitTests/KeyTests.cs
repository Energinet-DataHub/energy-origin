using System.Text;
using API.RegistryConnector;
using FluentAssertions;
using Google.Protobuf;
using NSec.Cryptography;
using Xunit;
using Xunit.Abstractions;

namespace API.UnitTests;

public class KeyTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public KeyTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void CanDoIt()
    {
        var key = Key.Create(SignatureAlgorithm.Ed25519, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        var export = key.Export(KeyBlobFormat.RawPrivateKey);

        export.Should().HaveCountGreaterThan(1);

        var importedKey = Key.Import(SignatureAlgorithm.Ed25519, export, KeyBlobFormat.RawPrivateKey, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        importedKey.Should().BeEquivalentTo(key);

        key.Should().NotBeEquivalentTo(Key.Create(SignatureAlgorithm.Ed25519));
    }

    [Fact]
    public void can_be_imported_from_string()
    {
        var key = Key.Create(SignatureAlgorithm.Ed25519, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        
        var export = key.Export(KeyBlobFormat.RawPrivateKey);

        var base64Str = ByteString.CopyFrom(export).ToBase64();

        testOutputHelper.WriteLine($"Key: {base64Str}");
        testOutputHelper.WriteLine($"PublicKey: {ByteString.CopyFrom(key.PublicKey.Export(KeyBlobFormat.RawPublicKey)).ToBase64()}");

        var byteString = ByteString.FromBase64(base64Str);

        var importedKey = Key.Import(SignatureAlgorithm.Ed25519, byteString.Span, KeyBlobFormat.RawPrivateKey, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        importedKey.PublicKey.Equals(key.PublicKey).Should().BeTrue();

        importedKey.Should().BeEquivalentTo(key);
    }

    [Fact]
    public void can_be_imported_from_const()
    {
        var byteString = ByteString.FromBase64("4jqufvs4Q0/r7/dEbxk/4dmrOkQZ1iY0ilqWmrFEcZs=");
        var key = Key.Import(SignatureAlgorithm.Ed25519, byteString.Span, KeyBlobFormat.RawPrivateKey);

        key.Should().NotBeNull();

        var publicByteString = ByteString.CopyFrom(key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
        publicByteString.ToBase64().Should().Be("sJuwdQ4TOarGjc3CyEJ5c37jJNOaH6PcOiyE1ge6+24=");
    }

    [Fact]
    public void works_with_IssuerKey()
    {
        var byteString = ByteString.FromBase64(IssuerKey.PrivateKey);
        var key = Key.Import(SignatureAlgorithm.Ed25519, byteString.Span, KeyBlobFormat.RawPrivateKey);

        key.Should().NotBeNull();

        var publicByteString = ByteString.CopyFrom(key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
        publicByteString.ToBase64().Should().Be(IssuerKey.PublicKey);
    }

    [Fact]
    public void pem_test()
    {
        var bs = ByteString.FromBase64(
            "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1DNENBUUF3QlFZREsyVndCQ0lFSUJhb2FjVHVWL05ub3ROQTBlVzJxbFJZZ3Q2WTRsaWlXSzV5VDRFZ3JKR20KLS0tLS1FTkQgUFJJVkFURSBLRVktLS0tLQo=");
        
        var import = Key.Import(SignatureAlgorithm.Ed25519, bs.Span, KeyBlobFormat.PkixPrivateKeyText);

        import.Should().NotBeNull();
    }
}
