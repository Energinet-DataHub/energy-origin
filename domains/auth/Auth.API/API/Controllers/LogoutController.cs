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
    public async Task<IActionResult> GetAsync(ICryptography cryptography, IDiscoveryCache discoveryCache, IOptions<OidcOptions> oidcOptions, ILogger<LogoutController> logger)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", "2"));
        }

        var requestUrl = new RequestUrl(discoveryDocument.EndSessionEndpoint);

        var url = requestUrl.CreateEndSessionUrl(
            idTokenHint: HttpContext.User.IdentityToken(cryptography),
            postLogoutRedirectUri: oidcOptions.Value.FrontendRedirectUri.AbsoluteUri
        );

        return RedirectPreserveMethod(url);
    }

    [HttpGet()]
    [Route("issue")]
    public IActionResult RemoveThis(ICryptography cryptography, IOptions<TokenOptions> options) => Ok(TokenIssuer.Issue(cryptography, options.Value, new TokenIssuer.Input("me", "a", "1"))); // FIXME: remove
}
