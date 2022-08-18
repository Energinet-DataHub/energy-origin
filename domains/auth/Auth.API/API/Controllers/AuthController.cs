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
    readonly ILogger<AuthController> logger;
    readonly IOidcProviders oidcProviders;
    readonly ICookieService cookieService;
    private readonly AuthOptions _authOptions;

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders, ICookieService cookieService, IOptions<AuthOptions> authOptions)
    {
        this.logger = logger;
        this.oidcProviders = oidcProviders;
        this.cookieService = cookieService;
        _authOptions = authOptions.Value;

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

        return oidcProviders.CreateAuthorizationUri(state);
    }

    [HttpGet]
    [Route("test/makeCookie")]
    public ActionResult TestLogin(string opaque_token)
    {
        var cookieOptions = cookieService.CreateCookieOptions(_authOptions.CookieCreateExpires);
        HttpContext.Response.Cookies.Append($"{_authOptions.CookieName}", $"{opaque_token}", cookieOptions);
        return Ok();
    }


    [HttpPost]
    [Route("auth/oidc/logout")]
    public ActionResult<LogoutResponse> Logout()
    {
        //1. fecth token -> valid opaque_token
        //2. if token -> delete token from database, logout from oidc
        //3. Set cookie with new values
        //4. give response -> success

        var token = HttpContext.Request.Headers[_authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            //delete opaque_token from DB
            //oidcProviders.Logout(token);
            Response.Cookies.Delete(_authOptions.CookieName);
        }

        return Ok(new LogoutResponse { success = true });
    }
}
