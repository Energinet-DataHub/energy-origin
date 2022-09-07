using System;
using System.Net;
using System.Net.Http;
using API.Configuration;
using API.Controllers.dto;
using API.Models;
using API.Services;
using API.Services.OidcProviders;
using API.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Categories;

namespace Tests.Services.OidcProviders;

[UnitTest]
public class TestSignaturGruppen
{
    private readonly Mock<ILogger<SignaturGruppen>> mockLogger = new();
    private readonly MockHttpMessageHandler handlerMock = new();
    private readonly Mock<IOptions<AuthOptions>> mockAuthOptions = new();
    private readonly Mock<ICryptography> cryptography = new();
    private readonly Mock<IJwkService> jwkService = new();

    private SignaturGruppen signaturGruppen;

    public TestSignaturGruppen()
    {
        mockAuthOptions
            .Setup(x => x.Value)
            .Returns(new AuthOptions
            {
                ServiceUrl = "http://foobar.com",
                OidcLoginCallbackPath = "/oidc/login/callback",
                OidcClientId = "OIDCCLIENTID",
                OidcClientSecret = "OIDCCLIENTSECRET",
                OidcUrl = "http://localhost:8080",
                AmrValues = "AMRVALUES"
            });

        signaturGruppen = new SignaturGruppen(
            mockLogger.Object,
            mockAuthOptions.Object,
            new HttpClient(handlerMock),
            cryptography.Object,
            jwkService.Object
        );
    }

    [Fact]
    public async void CanSendAReuestToSignaturGruppen()
    {
        var token = "test";

        handlerMock.Expect("/api/v1/session/logout")
            .WithPartialContent(token)
            .Respond(HttpStatusCode.OK);

        await signaturGruppen.Logout(token);

        handlerMock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async void CannotLogoutFromSignaturGruppenSoWeLogAMessage()
    {
        handlerMock.When("/api/v1/session/logout").Respond(HttpStatusCode.Forbidden);
        await signaturGruppen.Logout("test");

        mockLogger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Fact]
    public void RedirectSuccess()
    {
        const string expectedNextUrl =
            "http://localhost:8080?response_type=code&client_id=OIDCCLIENTID&redirect_uri=http%3A%2F%2Ftest.energioprindelse.dk" +
            "%2Fapi%2Fauth%2Foidc%2Flogin%2Fcallback&scope=openid,mitid,nemid,userinfo_token&state=foo%3D42&language=en&" +
            "idp_params=%7B%22nemid%22%3A%7B%22amr_values%22%3A%22AMRVALUES%22%7D%7D";

        var state = new AuthState
        {
            FeUrl = "http://test.energioprindelse.dk",
            ReturnUrl = "https://demo.energioprindelse.dk/dashboard"
        };

        cryptography
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns("foo=42");

        var res = signaturGruppen.CreateAuthorizationUri(state);

        Assert.Equal(expectedNextUrl, res.NextUrl);
    }

    [Fact]
    public async void FetchToken_Succes()
    {
        var expectedOidcTokenResponse = new OidcTokenResponse() { IdToken = "Test_id_token", AccessToken = "sd", ExpiresIn = 3600, TokenType = "Bearer", Scope = "openid nemid mitiduserinfo_token", UserinfoToken = "TEST_userinfo_token" };

        var code = "TESTCODE";

        var content = "code";

        handlerMock.When("/connect/token")
            .WithPartialContent(content)
            .Respond(HttpStatusCode.OK, "application/json", @"{""id_token"" : ""Test_id_token"",""access_token"" : ""sd"",""expires_in"" : 3600,""token_type"" : ""Bearer"",""scope"" : ""openid nemid mitid userinfo_token"",""userinfo_token"" : ""TEST_userinfo_token""}");

        var responseToken = await signaturGruppen.FetchToken(code);

        Assert.Equal(expectedOidcTokenResponse.IdToken, responseToken.IdToken);
    }

    [Fact]
    public void FetchToken_ServerResponseWithBadrequest_Fail()
    {
        var expectedOidcTokenResponse = new OidcTokenResponse() { IdToken = "Test_id_token", AccessToken = "sd", ExpiresIn = 3600, TokenType = "Bearer", Scope = "openid nemid mitiduserinfo_token", UserinfoToken = "TEST_userinfo_token" };

        var content = "code";

        handlerMock.When("/connect/token")
            .WithPartialContent(content)
            .Respond(HttpStatusCode.BadRequest, "application/json", @"{""error"": ""invalid_grant""}");

        //var responseToken = await signaturGruppen.FetchToken(code);
        //Assert.Equal(HttpStatusCode.BadRequest.ToString(), await signaturGruppen.FetchToken(code).Result.ToString();
    }

    [Theory]
    [InlineData("mitid_user_aborted", "https://bar?success=0&error_code=E1&error=User%20interrupted")]
    [InlineData("user_aborted", "https://bar?success=0&error_code=E1&error=User%20interrupted")]
    [InlineData("unknow_error", "https://bar?success=0&error_code=E0&error=Unknown%20error%20from%20Identity%20Provider")]
    public void OidcFlow_BuildNextUrlWhenUserAbortedOrUnknownError(string errorDescription, string expectedNextUrl)
    {
        var state = new AuthState
        {
            FeUrl = "https://foo",
            ReturnUrl = "https://bar"
        };

        var oidcCallbackParams = new OidcCallbackParams() { ErrorDescription = errorDescription };

        var nexturl = signaturGruppen.OnOidcFlowFailed(state, oidcCallbackParams);

        Assert.Equal(expectedNextUrl, nexturl.NextUrl);
    }

}
