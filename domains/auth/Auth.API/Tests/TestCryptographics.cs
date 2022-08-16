using API.Services;
using API.Models;
using Tests.Resources;
using Xunit;
using Xunit.Categories;
using System;

namespace Tests;

[UnitTest]
public sealed class TestCryptographics
{
    [Fact]
    public void Encrypt_state_success()
    {
        Environment.SetEnvironmentVariable("SecretKey", "mysmallkey123456");

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

    [Fact]
    public void Decrypt_state_success()
    {
        var feUrl = "http://test.energioprindelse.dk";
        var returnUrl = "https://demo.energioprindelse.dk/dashboard";

        var state = new AuthState()
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        var cryptoService = new CryptographyService();
        var encryptedState = cryptoService.EncryptState(state.ToString());

        var decryptedState = cryptoService.decryptState(encryptedState);

        Assert.NotNull(decryptedState);
        Assert.NotEmpty(decryptedState);
        Assert.IsType<string>(decryptedState);

    }


}
