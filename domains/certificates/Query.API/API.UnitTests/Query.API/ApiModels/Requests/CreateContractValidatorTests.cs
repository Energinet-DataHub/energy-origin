using System;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using FluentValidation.TestHelper;
using Xunit;

namespace API.UnitTests.Query.API.ApiModels.Requests;

public class CreateContractValidatorTests
{
    [Fact]
    public async Task Validate_StartDateNow_NoValidationError()
    {
        var validator = new CreateContractValidator();

        var now = DateTimeOffset.UtcNow;
        var result = await validator.TestValidateAsync(new CreateContract
        { GSRN = "123456789032432", StartDate = now.ToUnixTimeSeconds() });

        result.ShouldNotHaveValidationErrorFor(cc => cc.StartDate);
    }

    [Fact]
    public async Task Validate_StartDateOnMidnight_NoValidationError()
    {
        var validator = new CreateContractValidator();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var result = await validator.TestValidateAsync(new CreateContract
        { GSRN = "123456789032432", StartDate = utcMidnight.ToUnixTimeSeconds() });

        result.ShouldNotHaveValidationErrorFor(cc => cc.StartDate);
    }

    [Fact]
    public async Task Validate_StartDateJustBeforeMidnight_HaveValidationError()
    {
        var validator = new CreateContractValidator();

        var now = DateTimeOffset.UtcNow;
        var justBeforeUtcMidnight = now.Subtract(now.TimeOfDay).AddSeconds(-1);

        var result =
            await validator.TestValidateAsync(new CreateContract
            { GSRN = "123456789032432", StartDate = justBeforeUtcMidnight.ToUnixTimeSeconds() });

        result.ShouldHaveValidationErrorFor(cc => cc.StartDate);
    }

    [Fact]
    public async Task Validate_StartDateInMilliseconds_HaveValidationError()
    {
        var validator = new CreateContractValidator();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);
        var utcMidnightInMilliseconds = utcMidnight.ToUnixTimeMilliseconds();

        var result = await validator.TestValidateAsync(new CreateContract
            { GSRN = "123456789032432", StartDate = utcMidnightInMilliseconds });

        result.ShouldHaveValidationErrorFor(cc => cc.StartDate);
    }

    [Fact]
    public async Task Validate_StartDateInYear10000_HaveValidationError()
    {
        var validator = new CreateContractValidator();

        const long januaryFirstYear10000 = 253402300800L;

        var result = await validator.TestValidateAsync(new CreateContract
            { GSRN = "123456789032432", StartDate = januaryFirstYear10000 });

        result.ShouldHaveValidationErrorFor(cc => cc.StartDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a number")]
    [InlineData("42")]
    [InlineData("12345678901234567")]
    [InlineData("1234567890123456789")]
    [InlineData("Some18CharsLongStr")]
    [InlineData("1234567890 12345678")]
    [InlineData("123456789012345678 ")]
    [InlineData(" 123456789012345678")]
    public async Task Validate_InvalidGsrn_HaveValidationError(string invalidGsrn)
    {
        var validator = new CreateContractValidator();

        var result = await validator.TestValidateAsync(new CreateContract
        { GSRN = invalidGsrn, StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });

        result.ShouldHaveValidationErrorFor(cc => cc.GSRN);
    }

    [Theory]
    [InlineData("123456789012345678")]
    public async Task Validate_ValidGsrn_NoValidationError(string validGsrn)
    {
        var validator = new CreateContractValidator();

        var result = await validator.TestValidateAsync(new CreateContract
        { GSRN = validGsrn, StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });

        result.ShouldNotHaveValidationErrorFor(cc => cc.GSRN);
    }
}
