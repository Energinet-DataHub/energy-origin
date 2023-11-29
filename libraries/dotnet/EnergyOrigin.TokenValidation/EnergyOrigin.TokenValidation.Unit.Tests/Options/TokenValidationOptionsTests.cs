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
    [InlineData(null, "TestIssuer", "TestAudience")]
    [InlineData(new byte[0], null, "TestAudience")]
    [InlineData(new byte[0], "TestIssuer", null)]
    public void Properties_ShouldFailValidation_WhenRequiredPropertyIsNull(byte[] publicKey, string issuer, string audience)
    {
        var options = new TokenValidationOptions
        {
            PublicKey = publicKey,
            Issuer = issuer,
            Audience = audience
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options, null, null);
        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, true);

        Assert.False(isValid);
        Assert.NotEmpty(validationResults);
    }
}
