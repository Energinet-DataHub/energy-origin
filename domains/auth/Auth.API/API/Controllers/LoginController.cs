using API.Models.Entities;
using API.Options;
using API.Services;
using API.Values;
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
    public async Task<IActionResult> LoginAsync(IDiscoveryCache discoveryCache, IOptions<OidcOptions> oidcOptions, ILogger<LoginController> logger)
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
            scope: "openid mitid nemid ssn userinfo_token",
            extra: new Parameters(new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("idp_params","{\"nemid\": {\"amr_values\": \"nemid.otp nemid.keyfile\"}}")
            }));

        return RedirectPreserveMethod(url);
    }

    // FIXME: remove below ----

    [HttpGet]
    [Route("test")]
    public IActionResult GetConfigurationSettings(IConfiguration config) => Ok(config.GetSection("Token").Get<TokenOptions>());

    [HttpGet()]
    [Route("GetUserById/{id}", Name = "GetUserById")]
    public async Task<ActionResult<User>> GetUserByIdAsync([FromRoute] Guid id, [FromServices] IUserService userService)
    {
        var user = await userService.GetUserByIdAsync(id); return user is not null ? (ActionResult<User>)Ok(user) : (ActionResult<User>)NotFound();
    }

    [HttpGet()]
    [Route("GetUserByProviderId/{id}", Name = "GetUserByProviderId")]
    public ActionResult<User> GetUserByProviderIdAsync([FromRoute] string id, [FromServices] IUserService userService)
    {
        var user = userService.GetUserByProviderIdAsync(id); return user is not null ? (ActionResult<User>)Ok(user) : (ActionResult<User>)NotFound();
    }

    [HttpGet()]
    [Route("G")]
    public IActionResult GoGo(IHttpContextAccessor accessor)
    {
        accessor.HttpContext!.Response.Cookies.Append("Authentication", "token?", new CookieOptions
        {
            IsEssential = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(1))
        });

        return RedirectPreserveMethod("/dashboard");
    }

    [HttpGet()]
    [Route("F")]
    public IActionResult Fallback(IHttpContextAccessor accessor)
    {
        accessor.HttpContext!.Response.Cookies.Append("Authentication", "token?!", new CookieOptions
        {
            IsEssential = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(1))
        });

        return Ok($"""<html><head><meta http-equiv="refresh" content="0; URL='/dashboard'"/></head><body /></html>""");
    }
}
