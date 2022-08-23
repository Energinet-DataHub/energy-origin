using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using API.Configuration;
using Microsoft.Extensions.Options;

namespace API.Controllers;


[ApiController]
public class AuthController : ControllerBase
{
    readonly ILogger<AuthController> _logger;
    readonly IOidcProviders _oidcProviders;
    readonly ITokenStorage _tokenStorage;
    readonly ICookieService _cookieService;
    private readonly AuthOptions _authOptions;

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders, IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage, ICookieService cookieService)

    {
        _logger = logger;
        _oidcProviders = oidcProviders;
        _authOptions = authOptions.Value;
        _tokenStorage = tokenStorage;
        _cookieService = cookieService;
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
    [Route("test/makeCookie")]
    public ActionResult TestLogin()
    {
        var opaque_token = "test";
        var cookieOptions = _cookieService.CreateCookieOptions(_authOptions.CookieExpiresTimeDelta);
        HttpContext.Response.Cookies.Append($"{_authOptions.CookieName}", $"{opaque_token}", cookieOptions);
        return Ok();
    }

    [HttpGet]
    [Route("/token/forward-auth")]
    public ActionResult ValidateCookie()
    {
        var opaqueToken = HttpContext.Request.Headers[_authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken == null)
        {
            return Unauthorized();
        }

        var validCookie = _cookieService.IsValid();

        if (validCookie != "test" )
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpPost]
    [Route("/auth/logout")]
    public ActionResult<LogoutResponse> Logout()
    {
        var opaqueToken = HttpContext.Request.Headers[_authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken != null)
        {
            var idToken = _tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            //TODO _oidcProviders.Logout(idToken);
            _tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(_authOptions.CookieName);

        return Ok(new LogoutResponse { success = true });
    }
}
