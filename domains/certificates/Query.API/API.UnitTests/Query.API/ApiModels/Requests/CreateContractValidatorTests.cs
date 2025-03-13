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
        { GSRN = "123456789032432", StartDate = now.ToUnixTimeSeconds() }, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldNotHaveValidationErrorFor(cc => cc.StartDate);
    }

    [Fact]
    public async Task Validate_StartDateOnMidnight_NoValidationError()
    {
        var validator = new CreateContractValidator();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var result = await validator.TestValidateAsync(new CreateContract
        { GSRN = "123456789032432", StartDate = utcMidnight.ToUnixTimeSeconds() }, cancellationToken: TestContext.Current.CancellationToken);

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
            { GSRN = "123456789032432", StartDate = justBeforeUtcMidnight.ToUnixTimeSeconds() }, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(cc => cc.StartDate);
    }
}
