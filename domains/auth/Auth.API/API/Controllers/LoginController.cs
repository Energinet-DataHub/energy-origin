using API.Options;
using API.Utilities;
using API.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace API.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [Route("auth/login")]
    public async Task<IActionResult> LoginAsync(
        IDiscoveryCache discoveryCache,
        OidcOptions oidcOptions,
        IdentityProviderOptions providerOptions,
        ILogger<LoginController> logger,
        [FromQuery] string? state = default,
        [FromQuery] string? overrideRedirectionUri = default,
        [FromQuery] string? redirectionPath = default)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.FrontendRedirectUri.AbsoluteUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
        }
//Test push 5
        var (scope, arguments) = providerOptions.GetIdentityProviderArguments();

        var oidcState = new OidcState(
            State: state,
            RedirectionUri: overrideRedirectionUri,
            RedirectionPath: redirectionPath
        );
        var requestUrl = new RequestUrl(discoveryDocument.AuthorizeEndpoint);
        var url = requestUrl.CreateAuthorizeUrl(
            clientId: oidcOptions.ClientId,
            responseType: "code",
            redirectUri: oidcOptions.AuthorityCallbackUri.AbsoluteUri,
            state: oidcState.Encode(),
            prompt: "login",
            scope: scope,
            extra: new Parameters(arguments));

        return RedirectPreserveMethod(url);
    }
}
