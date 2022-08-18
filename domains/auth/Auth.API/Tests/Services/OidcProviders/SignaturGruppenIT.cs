using System;
using System.Net;
using System.Net.Http;
using API.Services;
using API.Services.OidcProviders;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Categories;

namespace Tests.Services.OidcProviders;

[UnitTest]
public class SignaturGruppenIT
{
    private readonly Mock<IOidcService> _mockOidcService = new();
    private readonly Mock<ILogger<SignaturGruppen>> _mockLogger = new();
    private readonly MockHttpMessageHandler _handlerMock = new();

    private SignaturGruppen _signaturGruppen;

    public SignaturGruppenIT()
    {
        _signaturGruppen = new SignaturGruppen(
            _mockLogger.Object,
            _mockOidcService.Object,
            new HttpClient(_handlerMock)
        );
    }

    [Fact]
    public async void can_logout_from_signaturgruppen()
    {
        Environment.SetEnvironmentVariable("OidcUrl", "http://localhost:8080");

        _handlerMock.When("/api/v1/session/logout").Respond(new HttpResponseMessage(HttpStatusCode.OK));
        await _signaturGruppen.Logout("test");

        _mockLogger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public async void cannot_logout_from_signaturgruppen()
    {
        Environment.SetEnvironmentVariable("OidcUrl", "http://localhost:8080");

        _handlerMock.When("/api/v1/session/logout").Respond(new HttpResponseMessage(HttpStatusCode.Forbidden));
        await _signaturGruppen.Logout("test");

        _mockLogger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
