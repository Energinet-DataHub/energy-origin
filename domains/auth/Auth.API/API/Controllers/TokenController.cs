using API.Configuration;
using API.Models;
using API.Services;
using API.TokenStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using API.Services.OidcProviders;

namespace API.Controllers;


[ApiController]
public class AuthController : ControllerBase
{
    readonly ITokenStorage tokenStorage;
    readonly ICookies cookies;
    private readonly AuthOptions authOptions;

    public AuthController(IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage, ICookies cookies)
    {
        this.authOptions = authOptions.Value;
        this.tokenStorage = tokenStorage;
        this.cookies = cookies;
    }

    [HttpGet]
    [Route("CreateTestCookie")]
    public ActionResult CreateTestToken()
    {
        var opaqueToken = "test";
        var cookieOptions = cookies.CreateCookieOptions(authOptions.CookieExpiresTimeDelta);
        HttpContext.Response.Cookies.Append($"{authOptions.CookieName}", $"{opaqueToken}", cookieOptions);
        return Ok();
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
