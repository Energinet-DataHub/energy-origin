using System.Threading.Tasks;
using API.Query.API.Controllers;
using FluentValidation.TestHelper;
using Xunit;

namespace API.UnitTests.Query.API;

public class CreateSignupValidatorTests
{
    [Fact]
    public async Task Test1()
    {
        var sut = new CreateSignupValidator();

        var result = await sut.TestValidateAsync(new CreateSignup("12345678901", 10));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
