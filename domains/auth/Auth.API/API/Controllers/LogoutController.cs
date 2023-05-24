using API.Options;
using API.Utilities;
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
    public async Task<IActionResult> LogoutAsync(Metrics metrics, IDiscoveryCache discoveryCache, IUserDescriptorMapper descriptorMapper, IOptions<OidcOptions> oidcOptions, ILogger<LogoutController> logger, [FromQuery] string? overrideRedirectionUri = default)
    {
        metrics.LogoutCounter.Add(
            1
        );

        var user = descriptorMapper.Map(User);

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

        var hint = user?.IdentityToken;
        if (hint == null)
        {
            return RedirectPreserveMethod(redirectionUri);
        }

        var url = requestUrl.CreateEndSessionUrl(
            idTokenHint: hint,
            postLogoutRedirectUri: redirectionUri
        );

        metrics.LogoutCounter.Add(
        1,
        new KeyValuePair<string, object?>("UserId", user?.Id),
        new KeyValuePair<string, object?>("CompanyId", user?.CompanyId),
        new KeyValuePair<string, object?>("IdentityProviderType", user?.ProviderType)
        );

        return RedirectPreserveMethod(url);
    }
}
