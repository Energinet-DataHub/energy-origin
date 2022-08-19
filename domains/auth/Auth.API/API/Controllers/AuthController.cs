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

    [HttpPost]
    [Route("/auth/logout")]
    public ActionResult<LogoutResponse> Logout()
    {

        var token = HttpContext.Request.Headers[_authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            //delete token from database
            //oidcProviders.Logout(token);
            Response.Cookies.Delete(_authOptions.CookieName);
        }

        return Ok(new LogoutResponse { success = true });
    }
}
