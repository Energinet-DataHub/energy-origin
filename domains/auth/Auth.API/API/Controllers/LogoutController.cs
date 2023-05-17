using API.Options;
using API.Utilities.Interfaces;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class LogoutController : ControllerBase
{
    [HttpGet()]
    [Route("auth/logout")]
    public async Task<IActionResult> LogoutAsync(IDiscoveryCache discoveryCache, IUserDescriptorMapper descriptorMapper, IOptions<OidcOptions> oidcOptions, ILogger<LogoutController> logger, [FromQuery] string? overrideRedirectionUri = default)
    {
        logger.LogInformation("This is a LogInformation deletion test");
        logger.LogWarning("This is a LogWarning deletion test");
        logger.LogError("This is a LogError deletion test");

        var redirectionUri = oidcOptions.Value.FrontendRedirectUri.AbsoluteUri;
        if (oidcOptions.Value.AllowRedirection && overrideRedirectionUri != null)
        {
            redirectionUri = overrideRedirectionUri;
        }

        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(redirectionUri);
        }

        var requestUrl = new RequestUrl(discoveryDocument.EndSessionEndpoint);

        var hint = descriptorMapper.Map(User)?.IdentityToken;
        if (hint == null)
        {
            return RedirectPreserveMethod(redirectionUri);
        }

        var url = requestUrl.CreateEndSessionUrl(
            idTokenHint: hint,
            postLogoutRedirectUri: redirectionUri
        );

        return RedirectPreserveMethod(url);
    }
}
