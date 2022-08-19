using API.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using API.Services.OidcProviders;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;


[ApiController]
public class AuthController : ControllerBase
{
    readonly IOidcProviders oidcProviders;

    public AuthController(IOidcProviders oidcProviders)
    {
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
        if (state.IdToken.IsNullOrEmpty())
        {
            return BadRequest(nameof(state.IdToken) + " must not be null");
        }

        oidcProviders.Logout(state.IdToken);
        return Ok();
    }
}
