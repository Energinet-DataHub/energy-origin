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

        var cryptoService = new CryptographyService();

        var encryptedState = cryptoService.EncryptState(state.ToString());

        Assert.NotNull(encryptedState);
        Assert.NotEmpty(encryptedState);
        Assert.IsType<string>(encryptedState);

        Span<byte> buffer = new Span<byte>(new byte[encryptedState.Length]);
        var base64DecodedState = Convert.TryFromBase64String(encryptedState, buffer, out int bytesParsed);

        Assert.True(base64DecodedState);
    }


}
