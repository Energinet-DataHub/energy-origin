using API.Query.API.ApiModels.Requests;
using System.Threading.Tasks;
using System;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace API.UnitTests.Query.API.ApiModels.Requests;

public class RuleExtensionsTests
{
    [Fact]
    public async Task MustBeBeforeYear10000_TimestampInSeconds_NoValidationError()
    {
        var validator = new InlineValidator<TestClass>();
        validator.RuleFor(c => c.Timestamp).MustBeBeforeYear10000();

        var now = DateTimeOffset.UtcNow;
        var nowInUnixTimeSeconds = now.ToUnixTimeSeconds();

        var result = await validator.TestValidateAsync(new TestClass { Timestamp = nowInUnixTimeSeconds });

        result.ShouldNotHaveValidationErrorFor(cc => cc.Timestamp);
    }

    [Fact]
    public async Task MustBeBeforeYear10000_TimestampInMilliseconds_HaveValidationError()
    {
        var validator = new InlineValidator<TestClass>();
        validator.RuleFor(c => c.Timestamp).MustBeBeforeYear10000();

        var now = DateTimeOffset.UtcNow;
        var nowInUnitTimeMilliseconds = now.ToUnixTimeMilliseconds();

        var result = await validator.TestValidateAsync(new TestClass { Timestamp = nowInUnitTimeMilliseconds });

        result.ShouldHaveValidationErrorFor(cc => cc.Timestamp);
    }

    [Fact]
    public async Task MustBeBeforeYear10000_TimestampInYear10000_HaveValidationError()
    {
        var validator = new InlineValidator<TestClass>();
        validator.RuleFor(c => c.Timestamp).MustBeBeforeYear10000();

        const long januaryFirstYear10000 = 253402300800L;

        var result = await validator.TestValidateAsync(new TestClass { Timestamp = januaryFirstYear10000 });

        result.ShouldHaveValidationErrorFor(cc => cc.Timestamp);
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
    public async Task MustBeValidGsrn_InvalidGsrn_HaveValidationError(string invalidGsrn)
    {
        var validator = new InlineValidator<TestClass>();
        validator.RuleFor(c => c.GSRN).MustBeValidGsrn();

        var result = await validator.TestValidateAsync(new TestClass { GSRN = invalidGsrn });

        result.ShouldHaveValidationErrorFor(cc => cc.GSRN);
    }

    [Theory]
    [InlineData("123456789012345678")]
    public async Task MustBeValidGsrn_ValidGsrn_NoValidationError(string validGsrn)
    {
        var validator = new InlineValidator<TestClass>();
        validator.RuleFor(c => c.GSRN).MustBeValidGsrn();

        var result = await validator.TestValidateAsync(new TestClass { GSRN = validGsrn });

        result.ShouldNotHaveValidationErrorFor(cc => cc.GSRN);
    }

    private class TestClass
    {
        public string GSRN { get; set; } = "";
        public long Timestamp { get; set; }
    }
}
