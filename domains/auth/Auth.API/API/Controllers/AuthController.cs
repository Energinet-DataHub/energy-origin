using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace API.Controllers;


[ApiController]
public class AuthController
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

    [HttpGet]
    [Route("/oidc/login/callback")]
    public NextStep Callback(
        [Required] string code,
        [Required] string state
        )
    {
        AuthState authState = JsonSerializer.Deserialize<AuthState>(state)!;



    }

}
