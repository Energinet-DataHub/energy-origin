using API.Configuration;
using API.Models;
using API.Services;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Text.Json;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class TestCryptographics
{
    [Fact]
    public void Encrypt_state_success()
    {
        var feUrl = "http://test.energioprindelse.dk";
        var returnUrl = "https://demo.energioprindelse.dk/dashboard";

        var state = new AuthState
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions { SecretKey = "mysmallkey123456" });

        var cryptoService = new CryptographyService(authOptionsMock.Object);

        var encryptedState = cryptoService.Encrypt(state.ToString());

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
        AddEnvironmentVariables.EnvironmentVariables();

        var feUrl = "http://test.energioprindelse.dk";
        var returnUrl = "https://demo.energioprindelse.dk/dashboard";

        var state = new AuthState
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        var serilizedJson = JsonSerializer.Serialize(state);

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions { SecretKey = "mysmallkey123456" });

        var cryptoService = new CryptographyService(authOptionsMock.Object);
        var encryptedState = cryptoService.Encrypt(serilizedJson);

        var decryptedState = cryptoService.Decrypt(encryptedState);

        Assert.NotNull(decryptedState);
        Assert.NotEmpty(decryptedState);
        Assert.IsType<string>(decryptedState);
        Assert.Equal(serilizedJson, decryptedState);
    }
}
