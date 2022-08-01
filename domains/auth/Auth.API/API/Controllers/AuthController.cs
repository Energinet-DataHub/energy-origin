using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Services;
using System.Text.Json;

namespace API.Controllers;


[ApiController]
public class AuthController
{
    readonly ILogger<AuthController> logger;
    readonly ISignaturGruppen signaturGruppen;

    [HttpGet]
    [Route("/api/auth/oidc/login")]
    public async Task<LoginResponse> Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {

        return await signaturGruppen.CreateRedirecthUrl(feUrl, returnUrl);
    }

}
