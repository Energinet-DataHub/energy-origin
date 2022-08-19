using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;


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

        return new NextStep();

    }

}
