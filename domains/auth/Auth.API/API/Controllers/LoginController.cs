using API.Models.Entities;
using API.Options;
using API.Repositories.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
    [HttpGet()]
    [Route("auth/login")]
    public async Task<IActionResult> LoginAsync(IDiscoveryCache discoveryCache, IdentityProviderOptions providerOptions, IOptions<OidcOptions> oidcOptions, ILogger<LoginController> logger)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, ErrorCode.QueryString, ErrorCode.AuthenticationUpstream.DiscoveryUnavailable));
        }

        var requestUrl = new RequestUrl(discoveryDocument.AuthorizeEndpoint);

        var url = requestUrl.CreateAuthorizeUrl(
            clientId: oidcOptions.Value.ClientId,
            responseType: "code",
            redirectUri: oidcOptions.Value.AuthorityCallbackUri.AbsoluteUri,
            scope: "openid mitid nemlogin nemid private_to_business nemid.pid ssn userinfo_token",
            extra: new Parameters(new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("idp_params", """{"nemid": {"amr_values": "nemid.otp nemid.keyfile"}, "mitid_erhverv": {"allow_private":true}}"""),
                new KeyValuePair<string, string>("idp_values", "nemid mitid mitid_erhverv")
            }));

        return RedirectPreserveMethod(url);
    }
}
