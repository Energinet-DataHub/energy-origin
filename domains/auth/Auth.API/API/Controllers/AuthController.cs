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
    private readonly AuthOptions _authOptions;

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders, IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage)
    {
        _logger = logger;
        _oidcProviders = oidcProviders;
        _authOptions = authOptions.Value;
        _tokenStorage = tokenStorage;
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

    [HttpPost]
    [Route("/auth/logout")]
    public ActionResult<LogoutResponse> Logout()
    {
        var token = HttpContext.Request.Headers[_authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            _tokenStorage.Delete(token);
            //oidcProviders.Logout(token);
        }

        Response.Cookies.Delete(_authOptions.CookieName);

        return Ok(new LogoutResponse { success = true });
    }
}
