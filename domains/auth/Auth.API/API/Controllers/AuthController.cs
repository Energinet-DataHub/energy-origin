using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Services; 

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
        
        return signaturGruppen.CreateRedirecthUrl(feUrl, returnUrl);
    }

}
