using System.ComponentModel.DataAnnotations;
using API.Configuration;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;


[ApiController]
public class AuthController : ControllerBase
{
    readonly ILogger<AuthController> logger;
    readonly IOidcProviders oidcProviders;
    readonly ITokenStorage tokenStorage;
    private readonly AuthOptions authOptions;

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders, IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage)
    {
        this.logger = logger;
        this.oidcProviders = oidcProviders;
        this.authOptions = authOptions.Value;
        this.tokenStorage = tokenStorage;
    }

    [HttpGet]
    [Route("/oidc/login")]
    public NextStep Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {
        var state = new AuthState()
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
        var opaqueToken = HttpContext.Request.Headers[authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken != null)
        {
            var idToken = tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            //TODO oidcProviders.Logout(idToken);
            tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(authOptions.CookieName);

        return Ok(new LogoutResponse { success = true });
    }
}
