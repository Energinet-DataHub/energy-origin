using System.Reflection;
using API.Controllers;
using API.Options;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Controllers;

public class LoginControllerTests
{
    private readonly LoginController sut;
    public LoginControllerTests()
    {
        sut = new LoginController();
    }

    [Fact]
    public async Task Index_ReturnsAViewResult_WithAListOfBrainstormSessions()
    {
        // Arrange
        var discoveryCacheMock = new Mock<IDiscoveryCache>();
        var discoveryDocumentResponseMock = new Mock<DiscoveryDocumentResponse>();
        var I = discoveryDocumentResponseMock.Object.GetType().GetProperty(nameof(discoveryDocumentResponseMock.Object.IsError), BindingFlags.Public | BindingFlags.Instance);
        I.SetValue(discoveryDocumentResponseMock.Object, false);
       // discoveryDocumentResponseMock.SetupGet(z=>z.IsError).Returns(false);
       // discoveryDocumentResponseMock.SetupGet(z => z.AuthorizeEndpoint).Returns("example.com");
        discoveryCacheMock.Setup(d => d.GetAsync()).ReturnsAsync(discoveryDocumentResponseMock.Object);

       



        var oidcOptions = Options.Create(new OidcOptions());
        oidcOptions.Value.AuthorityUrl = new Uri("example.com");
        oidcOptions.Value.CacheDuration = new TimeSpan(6, 0, 0);
        oidcOptions.Value.ClientId = "testClientId";
        oidcOptions.Value.RedirectUri = "example.com";
        var loggerMoq = Mock.Of<ILogger<LoginController>>();

        // Act
        var result = await sut.GetAsync(discoveryCacheMock.Object, oidcOptions, loggerMoq);
        var okResult = result as ObjectResult;

        // Assert
        Assert.NotNull(okResult);
        Assert.Equal(307, okResult.StatusCode);
    }
}
