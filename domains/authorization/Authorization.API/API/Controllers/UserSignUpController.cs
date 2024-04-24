using System;
using System.Threading.Tasks;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/user-signup")]
public class UserSignUpController(IUserSignUpService userSignUpService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SignUp([FromHeader(Name = "Authorization")] string authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
            return BadRequest("Authorization header is missing.");

        var token = authorizationHeader.Split(' ')[1];

        try
        {
            await userSignUpService.ProcessUserSignUpAsync(token);
            return Ok("User sign-up processed successfully.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
