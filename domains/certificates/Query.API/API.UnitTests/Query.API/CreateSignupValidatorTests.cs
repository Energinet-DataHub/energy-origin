using System;
using System.Threading.Tasks;
using API.Query.API.Controllers;
using FluentValidation.TestHelper;
using Xunit;

namespace API.UnitTests.Query.API;

public class CreateSignupValidatorTests
{
    [Fact]
    public async Task start_date_now()
    {
        var validator = new CreateSignupValidator();

        var now = DateTimeOffset.UtcNow;
        var result = await validator.TestValidateAsync(new CreateSignup("123456789032432", now.ToUnixTimeSeconds()));

        result.ShouldNotHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Fact]
    public async Task start_date_on_midnight()
    {
        var validator = new CreateSignupValidator();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var result = await validator.TestValidateAsync(new CreateSignup("123456789032432", utcMidnight.ToUnixTimeSeconds()));

        result.ShouldNotHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Fact]
    public async Task start_date_just_before_midnight()
    {
        var validator = new CreateSignupValidator();

        var now = DateTimeOffset.UtcNow;
        var justBeforeUtcMidnight = now.Subtract(now.TimeOfDay).AddSeconds(-1);

        var result =
            await validator.TestValidateAsync(new CreateSignup("123456789032432", justBeforeUtcMidnight.ToUnixTimeSeconds()));

        result.ShouldHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a number")]
    [InlineData("42")]
    [InlineData("12345678901234567")]
    [InlineData("1234567890123456789")]
    [InlineData("1234567890 12345678")]
    public async Task gsrn_not_valid(string invalidGsrn)
    {
        var validator = new CreateSignupValidator();

        var result = await validator.TestValidateAsync(new CreateSignup(invalidGsrn, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        result.ShouldHaveValidationErrorFor(signup => signup.Gsrn);
    }

    [Theory]
    [InlineData("123456789012345678")]
    [InlineData("123456789012345678 ")]
    [InlineData(" 123456789012345678")]
    public async Task gsrn_valid(string validGsrn)
    {
        var validator = new CreateSignupValidator();

        var result = await validator.TestValidateAsync(new CreateSignup(validGsrn, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        result.ShouldNotHaveValidationErrorFor(signup => signup.Gsrn);
    }

    //private static (CreateSignupValidator validator, MockHttpMessageHandler fakeHttpHandler) CreateValidator()
    //{
    //    var mock = new Mock<IHttpClientFactory>();
    //    var fakeHttpHandler = new MockHttpMessageHandler();
    //    var client = fakeHttpHandler.ToHttpClient();
    //    client.BaseAddress = new Uri("http://localhost:5000");
    //    mock.Setup(m => m.CreateClient("DataSync")).Returns(client);

    //    var validator = new CreateSignupValidator(mock.Object);
    //    return (validator, fakeHttpHandler);
    //}
}
