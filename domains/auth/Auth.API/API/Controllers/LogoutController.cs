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
    public async Task<IActionResult> LogoutAsync(IDiscoveryCache discoveryCache, IUserDescriptMapper descriptMapper, IOptions<OidcOptions> oidcOptions, ILogger<LogoutController> logger)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri);
        }

        var requestUrl = new RequestUrl(discoveryDocument.EndSessionEndpoint);

        var url = requestUrl.CreateEndSessionUrl(
            idTokenHint: descriptMapper.Map(User)?.IdentityToken,
            postLogoutRedirectUri: oidcOptions.Value.FrontendRedirectUri.AbsoluteUri
        );

        return RedirectPreserveMethod(url);
    }
}
