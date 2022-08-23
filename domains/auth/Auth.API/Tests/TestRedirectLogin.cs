using System.Text.Json;
using API.Configuration;
using API.Helpers;
using API.Models;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class TestRedirectLogin
{
    private readonly Mock<IOptions<AuthOptions>> authOptionsMock = new();
    private readonly Mock<ICryptography> _cryptography = new();

    public TestRedirectLogin()
    {
        authOptionsMock
            .Setup(x => x.Value)
            .Returns(new AuthOptions
            {
                OidcClientId = "OIDCCLIENTID",
                Scope = "SCOPE, SCOPE1",
                OidcUrl = "http://localhost",
                AmrValues = "AMRVALUES"
            });
    }
    [Fact]
    public void Oidc_Redirect_success()
    {
        //Arrange
        const string expectedNextUrl = "?response_type=code&client_id=OIDCCLIENTID&redirect_uri=http%3A%2F%2Ftest.energioprindelse.dk%2Fapi%2Fauth%2Foidc%2Flogin%2Fcallback&scope=SCOPE,%20SCOPE1&state=foo&language=en";

        var state = new AuthState
        {
            FeUrl = "http://test.energioprindelse.dk",
            ReturnUrl = "https://demo.energioprindelse.dk/dashboard"
        };


        _cryptography
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns("foo");

        //Act
        var res = CreateAuthorizationRedirectUrl("code", state, "en");

        //Assert
        Assert.Equal(expectedNextUrl, res.ToQueryString().Value);

    }
    private QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var serilizedJson = JsonSerializer.Serialize(state);
        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", "OIDCCLIENTID" },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", "SCOPE, SCOPE1" },
            { "state", _cryptography.Object.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }

    [Fact]
    public void SignaturGruppen_Redirect_success()
    {
        //Arrange
        const string expectedNextUrl =
            "http://localhost?response_type=code&client_id=OIDCCLIENTID&redirect_uri=http%3A%2F%2Ftest.energioprindelse.dk" +
            "%2Fapi%2Fauth%2Foidc%2Flogin%2Fcallback&scope=SCOPE,%20SCOPE1&state=foo%3D42&language=en&" +
            "idp_params=%7B%22nemid%22%3A%7B%22amr_values%22%3A%22AMRVALUES%22%7D%7D";

        var state = new AuthState
        {
            FeUrl = "http://test.energioprindelse.dk",
            ReturnUrl = "https://demo.energioprindelse.dk/dashboard"
        };

        _cryptography
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns("foo=42");

        authOptionsMock
            .Setup(x => x.Value)
            .Returns(new AuthOptions
            {
                OidcClientId = "OIDCCLIENTID",
                Scope = "SCOPE, SCOPE1",
                OidcUrl = "http://localhost",
                AmrValues = "AMRVALUES"
            });

        //Act
        var signaturGruppen = new SignaturGruppen(
            new Mock<ILogger<SignaturGruppen>>().Object,
            authOptionsMock.Object,
            new MockHttpMessageHandler().ToHttpClient(),
            _cryptography.Object
        );

        var res = signaturGruppen.CreateAuthorizationUri(state);

        //Assert
        Assert.Equal(expectedNextUrl, res.NextUrl);
    }
}
