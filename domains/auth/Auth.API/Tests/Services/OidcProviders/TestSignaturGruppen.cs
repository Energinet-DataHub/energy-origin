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
    private readonly Mock<IOidcService> _mockOidcService = new();
    private readonly Mock<ILogger<SignaturGruppen>> _mockLogger = new();
    private readonly MockHttpMessageHandler _handlerMock = new();
    private readonly Mock<IOptions<AuthOptions>> _mockAuthOptions = new();

    private SignaturGruppen _signaturGruppen;

    public TestSignaturGruppen()
    {
        _mockAuthOptions.Setup(a => a.Value).Returns(new AuthOptions
        {
            OidcUrl = "http://localhost:8080"
        });

        _signaturGruppen = new SignaturGruppen(
            _mockLogger.Object,
            _mockOidcService.Object,
            _mockAuthOptions.Object,
            new HttpClient(_handlerMock)
        );
    }

    [Fact]
    public async void CanSendAReuestToSignaturGruppen()
    {
        var token = "test";

        _handlerMock.Expect("/api/v1/session/logout")
            .WithPartialContent(token)
            .Respond(HttpStatusCode.OK);

        await _signaturGruppen.Logout(token);

        _handlerMock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async void CannotLogoutFromSignaturGruppenSoWeLogAMessage()
    {
        _handlerMock.When("/api/v1/session/logout").Respond(HttpStatusCode.Forbidden);
        await _signaturGruppen.Logout("test");

        _mockLogger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }
}
