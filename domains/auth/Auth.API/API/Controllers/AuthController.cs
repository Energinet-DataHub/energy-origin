using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers;


[ApiController]
public class AuthController
{
    readonly ILogger<AuthController> logger;
    readonly ISignaturGruppen signaturGruppen;

    public AuthController(ILogger<AuthController> logger, ISignaturGruppen signaturGruppen)
    {
        this.logger = logger;
        this.signaturGruppen = signaturGruppen;
    }

    [HttpGet]
    [Route("/api/auth/oidc/login")]
    public LoginResponse Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {
        AuthState state = new AuthState(feUrl, returnUrl)
        return signaturGruppen.NextStep(state);
    }

}
