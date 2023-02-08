using API.Options;
using API.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class LogoutController : ControllerBase
{
    [Authorize]
    [AllowAnonymous]
    [HttpGet()]
    [Route("auth/logout")]
    public async Task<IActionResult> LogoutAsync(IDiscoveryCache discoveryCache, IUserDescriptMapper thang, IOptions<OidcOptions> oidcOptions, ILogger<LogoutController> logger)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", "2"));
        }

        var requestUrl = new RequestUrl(discoveryDocument.EndSessionEndpoint);

        var url = requestUrl.CreateEndSessionUrl(
            idTokenHint: thang.IdentityToken,
            postLogoutRedirectUri: oidcOptions.Value.FrontendRedirectUri.AbsoluteUri
        );

        return RedirectPreserveMethod(url);
    }

    [HttpGet()]
    [Route("issue")]
    public async Task<IActionResult> RemoveThisAsync(ITokenIssuer tokenIssuer) => Ok(await tokenIssuer.IssueAsync("me", "a", "1")); // FIXME: remove
}
