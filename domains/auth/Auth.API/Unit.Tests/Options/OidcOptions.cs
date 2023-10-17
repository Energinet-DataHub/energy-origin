using System.ComponentModel.DataAnnotations;
using API.Options;

namespace Unit.Tests.Options;

public class OidcOptionsTests
{
    [Theory]
    [InlineData(true, OidcOptions.Generation.Predictable, OidcOptions.Redirection.Allow)]
    [InlineData(true, OidcOptions.Generation.Random, OidcOptions.Redirection.Allow)]
    [InlineData(true, OidcOptions.Generation.Predictable, OidcOptions.Redirection.Deny)]
    [InlineData(true, OidcOptions.Generation.Random, OidcOptions.Redirection.Deny)]
    [InlineData(false, OidcOptions.Generation.Invalid, OidcOptions.Redirection.Allow)]
    [InlineData(false, OidcOptions.Generation.Invalid, OidcOptions.Redirection.Deny)]
    [InlineData(false, OidcOptions.Generation.Predictable, OidcOptions.Redirection.Invalid)]
    [InlineData(false, OidcOptions.Generation.Random, OidcOptions.Redirection.Invalid)]
    public void Validate_ShouldReturnAsExpected_WhenArgumentsAreKnownToBeAsExpected(bool expected, OidcOptions.Generation generation, OidcOptions.Redirection redirection)
    {
        var result = OidcOptions.Validate(new OidcOptions()
        {
            IdGeneration = generation,
            RedirectionMode = redirection
        });

        Assert.Equal(expected, ValidationResult.Success == result);
    }
}
