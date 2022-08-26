using System.ComponentModel.DataAnnotations;
using API.Configuration;
using API.Models;
using API.Services;
using API.Services.OidcProviders;
using API.TokenStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;


[ApiController]
public class TokenController : ControllerBase
{
    readonly ITokenStorage tokenStorage;
    readonly ICookies cookies;
    private readonly AuthOptions authOptions;

    public TokenController(IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage, ICookies cookies)
    {
        this.authOptions = authOptions.Value;
        this.tokenStorage = tokenStorage;
        this.cookies = cookies;
    }

    [HttpGet]
    [Route("/token/forward-auth")]
    public ActionResult ForwardAuth()
    {
        var opaqueToken = HttpContext.Request.Cookies[authOptions.CookieName];

        if (opaqueToken == null || opaqueToken == "")
        {
            return Unauthorized();
        }

        var internalToken = tokenStorage.GetInteralTokenByOpaqueToken(opaqueToken);

        if (internalToken == null)
        {
            return Unauthorized();
        }

        HttpContext.Response.Headers.Add("Authorization", $"Bearer: {internalToken}");

        return Ok();
    }
}
