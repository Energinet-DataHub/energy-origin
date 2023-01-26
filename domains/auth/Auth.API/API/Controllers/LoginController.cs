using System.Web;
using API.Options;
using API.Utilities;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
    [HttpGet()]
    [Route("auth/login")]
    public async Task<IActionResult> GetAsync(IDiscoveryCache discoveryCache, IOptions<OidcOptions> oidcOptions, ILogger<LoginController> logger)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument.IsError)
        {
            logger.LogError(discoveryDocument.Error);
            var uri = new Uri(oidcOptions.Value.RedirectUri);
            return RedirectPreserveMethod(uri.AddQueryParameters("errorCode", "2").ToString());
        }

        var requestUrl = new RequestUrl(discoveryDocument.AuthorizeEndpoint);

        var url = requestUrl.CreateAuthorizeUrl(
            clientId: oidcOptions.Value.ClientId,
            responseType: "code",
            redirectUri: oidcOptions.Value.RedirectUri,
            scope: "openid mitid nemid userinfo_token",
            extra: new Parameters(new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("idp_params","{\"nemid\": {\"amr_values\": \"nemid.otp nemid.keyfile\"}}")
            }));

        return RedirectPreserveMethod(url);
    }
}

