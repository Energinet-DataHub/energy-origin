using System.Collections.Generic;
using API.Configuration;
using API.Models;
using API.Services.OidcProviders;
using API.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class TestRedirectLogin
{
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

        var cryptographyServiceMock = new Mock<ICryptographyService>();
        cryptographyServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns("foo");

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions { OidcClientId = "OIDCCLIENTID", Scope = "SCOPE, SCOPE1" });

        //Act
        var oidcService = new OidcService(new Mock<ILogger<OidcService>>().Object, cryptographyServiceMock.Object, authOptionsMock.Object);
        var res = oidcService.CreateAuthorizationRedirectUrl("code", state, "en");

        //Assert
        Assert.Equal(expectedNextUrl, res.ToQueryString().Value);

    }

    [Fact]
    public void SignaturGruppen_Redirect_success()
    {
        //Arrange
        const string expectedNextUrl = "?foo=42&idp_params=%7B%22nemid%22%3A%7B%22amr_values%22%3A%22AMRVALUES%22%7D%7D";

        var state = new AuthState
        {
            FeUrl = "http://test.energioprindelse.dk",
            ReturnUrl = "https://demo.energioprindelse.dk/dashboard"
        };

        var parameters = new[] { new KeyValuePair<string, string>("foo", "42") };

        var oidcServiceMock = new Mock<IOidcService>();
        oidcServiceMock
            .Setup(x => x.CreateAuthorizationRedirectUrl(It.IsAny<string>(), It.IsAny<AuthState>(), It.IsAny<string>()))
            .Returns(new QueryBuilder(parameters));

        var authOptionsMock = new Mock<IOptions<AuthOptions>>();
        authOptionsMock.Setup(x => x.Value).Returns(new AuthOptions { AmrValues = "AMRVALUES" });

        //Act
        var signaturGruppen = new SignaturGruppen(new Mock<ILogger<SignaturGruppen>>().Object, oidcServiceMock.Object, authOptionsMock.Object);

        var res = signaturGruppen.CreateAuthorizationUri(state);

        //Assert
        Assert.Equal(expectedNextUrl, res.NextUrl);
    }
}
