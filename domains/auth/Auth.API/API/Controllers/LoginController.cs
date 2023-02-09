using API.Models;
using API.Options;
using API.Services;
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
    public async Task<IActionResult> LoginAsync([FromServices] IDiscoveryCache discoveryCache, [FromServices] IOptions<OidcOptions> oidcOptions, [FromServices] ILogger<LoginController> logger)
    {
        var discoveryDocument = await discoveryCache.GetAsync();
        if (discoveryDocument == null || discoveryDocument.IsError)
        {
            logger.LogError("Unable to fetch discovery document: {Error}", discoveryDocument?.Error);
            return RedirectPreserveMethod(QueryHelpers.AddQueryString(oidcOptions.Value.FrontendRedirectUri.AbsoluteUri, "errorCode", "2"));
        }

        var requestUrl = new RequestUrl(discoveryDocument.AuthorizeEndpoint);

        var url = requestUrl.CreateAuthorizeUrl(
            clientId: oidcOptions.Value.ClientId,
            responseType: "code",
            redirectUri: oidcOptions.Value.AuthorityCallbackUri.AbsoluteUri,
            scope: "openid mitid nemid userinfo_token",
            extra: new Parameters(new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("idp_params","{\"nemid\": {\"amr_values\": \"nemid.otp nemid.keyfile\"}}")
            }));

        return RedirectPreserveMethod(url);
    }



    [HttpGet()]
    [Route("GetUserById/{id}", Name = "GetUserById")]
    public async Task<ActionResult<User>> GetUserById([FromRoute] Guid id, [FromServices] IUserService userService)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user is not null)
        {
            return Ok(user);
        }
        return NotFound();
    }

    [HttpGet()]
    [Route("GetUserByProviderId/{id}", Name = "GetUserByProviderId")]
    public async Task<ActionResult<User>> GetUserByProviderIdAsync([FromRoute] string id, [FromServices] IUserService userService)
    {
        var user = await userService.GetUserByProviderIdAsync(id);
        if (user is not null)
        {
            return Ok(user);
        }
        return NotFound();
    }

    [HttpGet()]
    [Route("G", Name = "G")]
    public async Task<ActionResult> G([FromServices] IUserService userService)
    {
    await userService.UpsertUserAsync(new User { ProviderId = "1", Name = "TestUser", AcceptedTermsVersion = 2, Tin = "hehe", AllowCPRLookup = false });
        return Ok();
    }


    [HttpGet()]
    [Route("W", Name = "W")]
    public async Task<ActionResult> W()
    {
        return Ok();
    }

}
