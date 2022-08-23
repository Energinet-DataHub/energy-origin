using System.ComponentModel.DataAnnotations;
using API.Models;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LoginController : ControllerBase
{
    readonly IOidcService _oidcService;

    public LoginController(IOidcService oidcService)
    {
        _oidcService = oidcService;
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

        return _oidcService.CreateAuthorizationUri(state);
    }
}
