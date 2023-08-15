using API.Converters;
using FluentAssertions;
using Google.Protobuf;
using ProjectOrigin.WalletSystem.V1;
using Xunit;

namespace API.UnitTests.Converters;

public class Base64ConverterTests
{
    [Fact]
    public void Base64Converter_ShouldBeAbleToConvertBackAfterConversion()
    {
        var wde = new WalletDepositEndpoint
        {
            Endpoint = "SomeEndpoint",
            PublicKey = ByteString.CopyFrom(new byte[10]),
            Version = 1
        };

        var base64String = Base64Converter.ConvertWalletDepositEndpointToBase64(wde);

        base64String.Should().NotBeNull();

        var obj = Base64Converter.ConvertToWalletDepositEndpoint(base64String);

        obj.Endpoint.Should().BeEquivalentTo(wde.Endpoint);
        obj.PublicKey.Should().BeEquivalentTo(wde.PublicKey);
        obj.Version.Should().Be(wde.Version);
    }
}
