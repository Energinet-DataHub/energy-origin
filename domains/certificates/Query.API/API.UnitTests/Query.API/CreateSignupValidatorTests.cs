using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using API.Query.API.Controllers;
using FluentValidation.TestHelper;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace API.UnitTests.Query.API;

public class CreateSignupValidatorTests
{
    [Fact]
    public async Task start_date_now()
    {
        var (validator, _) = CreateValidator();

        var now = DateTimeOffset.UtcNow;
        var result = await validator.TestValidateAsync(new CreateSignup("123456789032432", now.ToUnixTimeSeconds()));

        result.ShouldNotHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Fact]
    public async Task start_date_on_midnight()
    {
        var (validator, _) = CreateValidator();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var result = await validator.TestValidateAsync(new CreateSignup("123456789032432", utcMidnight.ToUnixTimeSeconds()));

        result.ShouldNotHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Fact]
    public async Task start_date_just_before_midnight()
    {
        var (validator, _) = CreateValidator();

        var now = DateTimeOffset.UtcNow;
        var justBeforeUtcMidnight = now.Subtract(now.TimeOfDay).AddSeconds(-1);

        var result =
            await validator.TestValidateAsync(new CreateSignup("123456789032432", justBeforeUtcMidnight.ToUnixTimeSeconds()));

        result.ShouldHaveValidationErrorFor(signup => signup.StartDate);
    }

    [Fact]
    public async Task Test1()
    {
        var (sut, client) = CreateValidator();

        client.Expect("/meteringPoints")
            .Respond(HttpStatusCode.NoContent);

        //var sut = new CreateSignupValidator(factory.Object);

        var result = await sut.TestValidateAsync(new CreateSignup("12345678901", 1000));

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static (CreateSignupValidator validator, MockHttpMessageHandler fakeHttpHandler) CreateValidator()
    {
        var mock = new Mock<IHttpClientFactory>();
        var fakeHttpHandler = new MockHttpMessageHandler();
        var client = fakeHttpHandler.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost:5000");
        mock.Setup(m => m.CreateClient("DataSync")).Returns(client);

        var validator = new CreateSignupValidator(mock.Object);
        return (validator, fakeHttpHandler);
    }
}
