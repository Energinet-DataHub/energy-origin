using System.ComponentModel.DataAnnotations;
using EnergyOrigin.TokenValidation.Options;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Options;

public class TokenValidationOptionsTests
{
    [Fact]
    public void Properties_ShouldBeInitializedAsNull()
    {
        var options = new TokenValidationOptions();

        Assert.Null(options.PublicKey);
        Assert.Null(options.Issuer);
        Assert.Null(options.Audience);
    }

    [Fact]
    public void Properties_ShouldBeCorrectlyInitialized()
    {
        var publicKey = new byte[] { 1, 2, 3 };
        var issuer = "TestIssuer";
        var audience = "TestAudience";

        var options = new TokenValidationOptions
        {
            PublicKey = publicKey,
            Issuer = issuer,
            Audience = audience
        };

        Assert.Equal(publicKey, options.PublicKey);
        Assert.Equal(issuer, options.Issuer);
        Assert.Equal(audience, options.Audience);
    }

    [Theory]
    [InlineData(null, "ValidIssuer", "ValidAudience")]
    [InlineData(new byte[0], null, "ValidAudience")]
    [InlineData(new byte[0], "ValidIssuer", null)]
    public void Properties_ShouldFailValidation_WhenRequiredPropertyIsNull(byte[]? testPublicKey, string? testIssuer, string? testAudience)
    {
        var options = new TokenValidationOptions
        {
            PublicKey = testPublicKey!,
            Issuer = testIssuer!,
            Audience = testAudience!
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options, null, null);

        var isValidationSuccessful = Validator.TryValidateObject(options, validationContext, validationResults, true);

        Assert.False(isValidationSuccessful);
        Assert.DoesNotContain(ValidationResult.Success, validationResults);
    }
}
