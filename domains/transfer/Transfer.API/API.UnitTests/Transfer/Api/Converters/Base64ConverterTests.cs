using API.Transfer.Api.Converters;
using FluentAssertions;
using Google.Protobuf;
using ProjectOrigin.WalletSystem.V1;
using Xunit;

namespace API.UnitTests.Transfer.Api.Converters;

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

    [Theory]
    [InlineData(null)]
    [InlineData("Regular string in disguise as base64 wallet-deposit-endpoint")]
    [InlineData("W3sibmFtZSI6ICJKb2huIn0sIHsibmFtZSI6ICJKYW5lIn1d")]
    [InlineData("Jane Doe")]
    public void TryConvertWalletDepositEndpoint_ShouldReturnFalse_WhenNotConvertible(string base64String)
    {
        var result = Base64Converter.TryConvertToWalletDepositEndpoint(base64String, out var wde);

        result.Should().BeFalse();
        wde.Should().BeNull();
    }

    [Fact]
    public void TryConvertWalletDepositEndpoint_ShouldReturnTrue_WhenConvertible()
    {
        var result = Base64Converter.TryConvertToWalletDepositEndpoint(Some.Base64EncodedWalletDepositEndpoint, out var wde);

        result.Should().BeTrue();
        wde.Should().NotBeNull();
    }
}
