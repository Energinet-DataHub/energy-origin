using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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
        [Required] string fe_url,
        [Required] string return_url,
        [Required] string type)
    {
        AuthState state = new AuthState()
        {
            FeUrl = fe_url,
            ReturnUrl = return_url,
            CustomerType = type
        };

        return _oidcProviders.CreateAuthorizationUri(state);
    }

    [HttpGet]
    [Route("/oidc/login/callback")]
    public NextStep Callback(
        [Required] string code,
        [Required] string state
        )
    {
        AuthState authState = JsonSerializer.Deserialize<AuthState>(state)!;

        return new NextStep();

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
