using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using API.Services.OidcProviders;

namespace API.Controllers;


[ApiController]
public class AuthController : ControllerBase
{
    readonly ILogger<AuthController> logger;
    readonly IOidcProviders oidcProviders;

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders)
    {
        this.logger = logger;
        this.oidcProviders = oidcProviders;
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
    [Route("/invalidate")]
    public IActionResult Invalidate([Required] AuthState state)
    {
        if (state.IdToken == null)
        {
            return BadRequest();
        }

        oidcProviders.Logout(state.IdToken);
        return Ok();
    }
}
