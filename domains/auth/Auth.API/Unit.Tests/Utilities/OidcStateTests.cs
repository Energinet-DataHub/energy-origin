
using System.Text;
using System.Text.Json;
using API.Utilities;

namespace Tests.Utilities;

public class OidcStateTests
{
    [Theory]
    [InlineData("WW91IGFyZSBhIGN1cmlvdXMgb25lLiBJIGxpa2UgeW91IDopCg==", "https://example.com/")]
    [InlineData("WW91IGFyZSBhIGN1cmlvdXMgb25lLiBJIGxpa2UgeW91IDopCg==", null)]
    [InlineData(null, "https://example.com/")]
    [InlineData(null, null)]
    public void OidcState_ShouldBeDecodable_WhenEncoded(string? frontendState, string? uri)
    {
        var state = new OidcState(State: frontendState, RedirectionUri: uri);
        var encoded = state.Encode();
        var decoded = OidcState.Decode(encoded);

        Assert.NotNull(decoded);
        Assert.Equal(frontendState, decoded.State);
        Assert.Equal(uri, decoded.RedirectionUri);
    }

    [Fact]
    public void OidcState_ShouldReturnNull_WhenGivenNull()
    {
        var decoded = OidcState.Decode(null);

        Assert.Null(decoded);
    }

    [Fact]
    public void OidcState_ShouldReturnStateWithNulls_WhenGivenEmptyObject()
    {
        var decoded = OidcState.Decode(Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")));

        Assert.NotNull(decoded);
        Assert.Null(decoded.State);
        Assert.Null(decoded.RedirectionUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bananana")]
    [InlineData("WW91IGFyZSBhIGN1cmlvdXMgb25lLiBJIGxpa2UgeW91IDopCg==")]
    public void OidcState_ShouldThrowJsonException_WhenGivenNonsense(string nonsense) => Assert.Throws<JsonException>(() => OidcState.Decode(nonsense));

    [Theory]
    [InlineData("{}")]
    [InlineData("Hello")]
    [InlineData("https://example.com/")]
    public void OidcState_ShouldThrowFormatException_WhenGivenNonsense(string nonsense) => Assert.Throws<FormatException>(() => OidcState.Decode(nonsense));
}
