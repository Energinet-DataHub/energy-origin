using System;
using System.Net;
using System.Net.Http;
using API.Configuration;
using API.Helpers;
using API.Models;
using API.Services;
using API.Services.OidcProviders;
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
                OidcClientId = "OIDCCLIENTID",
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
            "%2Fapi%2Fauth%2Foidc%2Flogin%2Fcallback&scope=SCOPE,%20SCOPE1&state=foo%3D42&language=en&" +
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
}
