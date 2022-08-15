using API.Services;
using API.Models;
using Tests.Resources;
using Xunit;
using Xunit.Categories;
using Moq;
using System;

namespace Tests;

[UnitTest]
public sealed class TestCryptographics
{
    private readonly Mock<ICryptographyService> _cryptographyService;

    public TestCryptographics()
    {
        _cryptographyService = new Mock<ICryptographyService>();
    }

    [Fact]
    public void Encrypt_state_success()
    {
        AddEnvironmentVariables.EnvironmentVariables();

        var feUrl = "http://test.energioprindelse.dk";
        var returnUrl = "https://demo.energioprindelse.dk/dashboard";

        var state = new AuthState()
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        var expectedString = "EPday0q/NJZf8i2sVT9+gMDPBw+srbjukn9cA6M5KGYuE2i8N7G0RraBYG5DTrtc9LbiZMXXYDjSz8G5kEhmzpIB0+EjLbCatAuBAYO9lvzpyzJzLmoR+tKkFxuMz4rsBK8ZR2JPb3WTIx3iKp+lZY/CjPBILHOqhwLMVIwc3mOBLw1MsVog2ent0rVxmwbFZVNznxQeBkNRZ6mHFvxuNTykoEpiirvO5z/5QA8LpZTIQWFX2J1opNthm2eo7qaSn4caZ6Qb2+UN6QiC0SoYI7XcskIHVsQCoorWMQvKnSQ5wV/Hiw6KAUNhXhceRCrf";


        _cryptographyService.Setup(x => x.EncryptState(state))
            .Returns(expectedString);

        var encryptedState = _cryptographyService.Object.EncryptState(state);

        Assert.NotNull(encryptedState);
        Assert.NotEmpty(encryptedState);
        Assert.IsType<string>(encryptedState);

        Span<byte> buffer = new Span<byte>(new byte[encryptedState.Length]);
        var base64DecodedState = Convert.TryFromBase64String(encryptedState, buffer, out int bytesParsed);

        Assert.True(base64DecodedState);
    }


}
