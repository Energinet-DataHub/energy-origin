using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using API.Models;


namespace API.Controllers;


[ApiController]
public class AuthController
{
    readonly ILogger<AuthController> logger;


    public async Task<LoginResponse> Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {
        var nestUrl = "sdhfljsdhflsdhflkjsd";
        return nestUrl;

    }

}
