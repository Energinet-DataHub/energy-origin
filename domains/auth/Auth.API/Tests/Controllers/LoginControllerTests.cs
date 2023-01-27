using System.Reflection;
using System.Text.Json;
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
    [Fact]
    public async Task Index_ReturnsAViewResult_WithAListOfBrainstormSessions()
    {
        var json = JsonDocument.Parse("""{"authorization_endpoint":"http://example.com"}""")!.RootElement;

        var document = new DiscoveryDocumentResponse();
        var reflection = document.GetType().GetProperty(nameof(document.Json), BindingFlags.Public | BindingFlags.Instance);
        reflection!.SetValue(document, json);

        var cache = Mock.Of<IDiscoveryCache>();
        _ = Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var oidcOptions = Options.Create(new OidcOptions());
        oidcOptions.Value.AuthorityUrl = new Uri("http://example.com");
        oidcOptions.Value.CacheDuration = new TimeSpan(6, 0, 0);
        oidcOptions.Value.ClientId = "testClientId";
        oidcOptions.Value.RedirectUri = "example.com";
        var logger = Mock.Of<ILogger<LoginController>>();

        var result = await new LoginController().GetAsync(cache, oidcOptions, logger);
        var okResult = result as ObjectResult;

        Assert.NotNull(okResult);
        Assert.Equal(307, okResult.StatusCode);
    }
}
