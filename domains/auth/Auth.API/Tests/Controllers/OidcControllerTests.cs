using System.Web;
using API.Controllers;
using API.Options;
using API.Services;
using API.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;

namespace Tests.Controllers;

public class OidcControllerTests
{
    private readonly OidcOptions oidcOptions;
    private readonly IDiscoveryCache cache = Mock.Of<IDiscoveryCache>();
    private readonly IUserDescriptMapper mapper = Mock.Of<IUserDescriptMapper>();
    private readonly IUserService service = Mock.Of<IUserService>();
    private readonly ITokenIssuer issuer = Mock.Of<ITokenIssuer>();
    private readonly ILogger<OidcController> logger = Mock.Of<ILogger<OidcController>>();

    public OidcControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", false)
            .Build();

        oidcOptions = configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithCookie_WhenInvoked()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>() { new("authorization_endpoint", $"http://{options.Value.AuthorityUri.Host}/connect") });

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(cache, new HttpClient(), mapper, service, issuer, options, logger, null, null, null);

        Assert.Fail("Not done yet");
    }

    [Fact]
    public async Task CallbackAsync_ShouldReturnRedirectToFrontendWithError_WhenCodeIsMissing()
    {
        var options = TestOptions.Oidc(oidcOptions);

        var document = DiscoveryDocument.Load(new List<KeyValuePair<string, string>>());

        Mock.Get(cache).Setup(it => it.GetAsync()).ReturnsAsync(document);

        var result = await new OidcController().CallbackAsync(cache, new MockHttpMessageHandler().ToHttpClient(), mapper, service, issuer, options, logger, null, null, null);

        Assert.NotNull(result);
        Assert.IsType<RedirectResult>(result);

        var redirectResult = (RedirectResult)result;
        Assert.True(redirectResult.PreserveMethod);
        Assert.False(redirectResult.Permanent);

        var uri = new Uri(redirectResult.Url);
        Assert.Equal(options.Value.FrontendRedirectUri.Host, uri.Host);

        var query = HttpUtility.UrlDecode(uri.Query);
        Assert.Contains($"errorCode=2", query); // FIXME: codable error list?
    }
}
