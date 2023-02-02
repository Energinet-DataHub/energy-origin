using API.Options;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class LogoutController : ControllerBase
{
    [HttpGet()]
    [Route("auth/logout")]
    public async Task<IActionResult> LogoutUser(IDiscoveryCache discoveryCache, IOptions<OidcOptions> oidcOptions, ILogger<LogoutController> logger)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", "2"));
        }

        var requestUrl = new RequestUrl(discoveryDocument.EndSessionEndpoint);

        var url = requestUrl.CreateEndSessionUrl(
            idTokenHint: null, // TODO: this should be the identity token obtained during code exchange
            postLogoutRedirectUri: oidcOptions.Value.FrontendRedirectUri.AbsoluteUri
        );

        return RedirectPreserveMethod(url);
    }
}
