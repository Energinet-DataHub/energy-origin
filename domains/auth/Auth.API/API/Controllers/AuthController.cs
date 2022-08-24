using API.Configuration;
using API.Models;
using API.Services;
using API.TokenStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers;


[ApiController]
public class AuthController : ControllerBase
{
    readonly ILogger<AuthController> _logger;
    readonly IOidcProviders _oidcProviders;
    readonly ITokenStorage _tokenStorage;
    readonly ICookies _cookies;
    private readonly AuthOptions _authOptions;

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders, IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage, ICookies cookies)
    {
        _logger = logger;
        _oidcProviders = oidcProviders;
        _authOptions = authOptions.Value;
        _tokenStorage = tokenStorage;
        _cookies = cookies;
    }

    [HttpGet]
    [Route("/oidc/login")]
    public NextStep Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {
        AuthState state = new AuthState()
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        return _oidcProviders.CreateAuthorizationUri(state);
    }

    [HttpGet]
    [Route("CreateTestCookie")]
    public ActionResult CreateTestToken()
    {
        var opaque_token = "test";
        var cookieOptions = _cookies.CreateCookieOptions(_authOptions.CookieExpiresTimeDelta);
        HttpContext.Response.Cookies.Append($"{_authOptions.CookieName}", $"{opaque_token}", cookieOptions);
        return Ok();
    }

    [HttpGet]
    [Route("/token/forward-auth")]
    public ActionResult ForwardAuth()
    {
        var opaqueToken = HttpContext.Request.Cookies[_authOptions.CookieName];

        if (opaqueToken == null || opaqueToken == "")
        {
            return Unauthorized();
        }

        var internalToken = _tokenStorage.GetInteralTokenByOpaqueToken(opaqueToken);

        if (internalToken == null)
        {
            return Unauthorized();
        }

        HttpContext.Response.Headers.Add("Authorization", $"Bearer: {internalToken}");

        return Ok();
    }

    [HttpPost]
    [Route("/auth/logout")]
    public ActionResult<LogoutResponse> Logout()
    {
        var opaqueToken = HttpContext.Request.Cookies[_authOptions.CookieName];

        if (opaqueToken != null)
        {
            //var idToken = _tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            //TODO _oidcProviders.Logout(idToken);
            //_tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(_authOptions.CookieName);

        return Ok(new LogoutResponse { success = true });
    }
}
