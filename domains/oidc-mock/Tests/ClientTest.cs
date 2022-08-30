using Oidc.Mock.Models;
using Xunit;

namespace Tests;

public class ClientTest
{
    [Theory]
    [InlineData("id", "secret", "http://example.com", true)]
    [InlineData("not-id", "secret", "http://example.com", false)]
    [InlineData("id", "not-secret", "http://example.com", false)]
    [InlineData("id", "secret", "http://not-example.com", false)]
    [InlineData("id", "secret", "http://localhost", true)]
    [InlineData("id", "secret", "https://localhost", true)]
    [InlineData("id", "secret", "http://localhost:1234", true)]
    [InlineData("id", "secret", "https://localhost:1234", true)]
    [InlineData("id", "secret", "http://LOCALHOST", true)]
    [InlineData("id", "secret", "https://LOCALHOST", true)]
    public void Validate(string inputId, string inputSecret, string inputRedirectUri, bool expected)
    {
        var client = new Client("id", "secret", "http://example.com");
        var (isValid, _) = client.Validate(inputId, inputSecret, inputRedirectUri);
        Assert.Equal(expected, isValid);
    }
}
