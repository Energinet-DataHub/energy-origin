using System.Text.RegularExpressions;
using FluentAssertions;

namespace API.UnitTests.ValueObjects;

public class AcrValuesRegexMatcherTests
{
    private const string Pattern = @"^(?!(?:.*(?<!\S)ett:login:type:(?:trial|normal)(?!\S).*(?<!\S)ett:login:type:(?:trial|normal)(?!\S)|.*(?<!\S)ett:login:type:(?!(?:trial|normal)(?!\S))\S*(?!\S))).*?(?<!\S)ett:login:type:(?:trial|normal)(?!\S).*$";

    [Theory]
    [InlineData("ett:login:type:trial", true)]
    [InlineData("ett:login:type:normal", true)]
    [InlineData("ett:login:type:trial urn:ett:location:dk", true)]
    [InlineData("urn:ett:location:dk ett:login:type:normal", true)]
    [InlineData("ett:login:type:trial urn:ett:location:eu extra:value", true)]

    // Invalid: multiple valid types
    [InlineData("ett:login:type:trial ett:login:type:normal", false)]
    [InlineData("ett:login:type:trial ett:login:type:trial", false)]

    // Invalid: invalid type
    [InlineData("ett:login:type:xyz", false)]
    [InlineData("ett:login:type:normal ett:login:type:xyz", false)]

    // Invalid: embedded token
    [InlineData("123:ett:login:type:trial", false)]
    [InlineData("ett:login:type:normal:ett:login:type:trial", false)]
    [InlineData("urn:ett:login:type:trial", false)]
    [InlineData("ett:login:type:trial:extra", false)]

    // Invalid: none provided
    [InlineData("urn:ett:location:dk", false)]
    [InlineData("", false)]

    // Valid: surrounded by whitespace
    [InlineData(" ett:login:type:trial ", true)]
    [InlineData("\tett:login:type:normal\n", true)]

    // Invalid: valid token as a substring
    [InlineData("myett:login:type:trial", false)]
    [InlineData("ett:login:type:trialist", false)]

    // Invalid: valid token with special characters
    [InlineData("ett:login:type:trial!", false)]
    [InlineData("ett:login:type:normal#", false)]

    // Valid: valid token with multiple unrelated tokens
    [InlineData("foo bar ett:login:type:trial baz", true)]

    // Invalid: valid token with leading or trailing colons
    [InlineData(":ett:login:type:trial", false)]
    [InlineData("ett:login:type:normal:", false)]

    // Valid: valid token in the middle of unrelated tokens
    [InlineData("foo ett:login:type:normal bar", true)]

    public void Regex_Should_Match_Expected_Login_Type_Format(string input, bool expectedMatch)
    {
        var match = Regex.IsMatch(input, Pattern);
        match.Should().Be(expectedMatch, $"Expected '{input}' to {(expectedMatch ? "match" : "not match")}.");
    }
}
