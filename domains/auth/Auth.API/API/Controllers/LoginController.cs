using System.ComponentModel.DataAnnotations;
using API.Models;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LoginController : ControllerBase
{
    readonly IOidcService oidcService;

    public LoginController(IOidcService oidcService)
    {
        this.oidcService = oidcService;
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

        return oidcService.CreateAuthorizationUri(state);
    }
}
