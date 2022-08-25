using System;
using System.Net;
using System.Net.Http;
using API.Configuration;
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
    private readonly Mock<IOidcService> mockOidcService = new();
    private readonly Mock<ILogger<SignaturGruppen>> mockLogger = new();
    private readonly MockHttpMessageHandler handlerMock = new();
    private readonly Mock<IOptions<AuthOptions>> mockAuthOptions = new();

    private SignaturGruppen signaturGruppen;

    public TestSignaturGruppen()
    {
        mockAuthOptions.Setup(a => a.Value).Returns(new AuthOptions
        {
            OidcUrl = "http://localhost:8080"
        });

        signaturGruppen = new SignaturGruppen(
            mockLogger.Object,
            mockOidcService.Object,
            mockAuthOptions.Object,
            new HttpClient(handlerMock)
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
}
