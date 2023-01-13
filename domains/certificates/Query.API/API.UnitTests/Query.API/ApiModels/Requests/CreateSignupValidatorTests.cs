using System;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using FluentValidation.TestHelper;
using Xunit;

namespace API.UnitTests.Query.API.ApiModels.Requests;

public class CreateSignupValidatorTests
{
    [Fact]
    public async Task Validate_StartDateNow_NoValidationError()
    {
        var validator = new CreateSignupValidator();

        var now = DateTimeOffset.UtcNow;
        var result = await validator.TestValidateAsync(new CreateSignup
        { GSRN = "123456789032432", StartDate = now.ToUnixTimeSeconds() });

        result.ShouldNotHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Fact]
    public async Task Validate_StartDateOnMidnight_NoValidationError()
    {
        var validator = new CreateSignupValidator();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var result = await validator.TestValidateAsync(new CreateSignup
        { GSRN = "123456789032432", StartDate = utcMidnight.ToUnixTimeSeconds() });

        result.ShouldNotHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Fact]
    public async Task Validate_StartDateJustBeforeMidnight_HaveValidationError()
    {
        var validator = new CreateSignupValidator();

        var now = DateTimeOffset.UtcNow;
        var justBeforeUtcMidnight = now.Subtract(now.TimeOfDay).AddSeconds(-1);

        var result =
            await validator.TestValidateAsync(new CreateSignup
            { GSRN = "123456789032432", StartDate = justBeforeUtcMidnight.ToUnixTimeSeconds() });

        result.ShouldHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a number")]
    [InlineData("42")]
    [InlineData("12345678901234567")]
    [InlineData("1234567890123456789")]
    [InlineData("1234567890 12345678")]
    public async Task Validate_InvalidGsrn_HaveValidationError(string invalidGsrn)
    {
        var validator = new CreateSignupValidator();

        var result = await validator.TestValidateAsync(new CreateSignup
        { GSRN = invalidGsrn, StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });

        result.ShouldHaveValidationErrorFor(signup => signup.GSRN);
    }

    [Theory]
    [InlineData("123456789012345678")]
    [InlineData("123456789012345678 ")]
    [InlineData(" 123456789012345678")]
    public async Task Validate_ValidGsrn_NoValidationError(string validGsrn)
    {
        var validator = new CreateSignupValidator();

        var result = await validator.TestValidateAsync(new CreateSignup
        { GSRN = validGsrn, StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });

        result.ShouldNotHaveValidationErrorFor(signup => signup.GSRN);
    }
}
