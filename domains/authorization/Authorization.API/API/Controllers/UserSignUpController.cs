using System;
using System.Threading.Tasks;
using API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/user-signup")]
public class UserSignUpController(IUserSignUpService userSignUpService) : ControllerBase
{
    /// <summary>
    /// Processes user sign-up.
    /// </summary>
    /// <param name="authorizationHeader">The Authorization header containing the JWT token.</param>
    /// <returns>A response indicating the outcome of the sign-up process.</returns>
    /// <response code="201">User sign-up processed successfully.</response>
    /// <response code="400">Bad request. The Authorization header is missing or invalid.</response>
    /// <response code="401">Unauthorized. The provided token is invalid or expired.</response>
    /// <response code="500">Internal server error. An error occurred while processing the sign-up.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SignUp([FromHeader(Name = "Authorization")] string authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return BadRequest("Authorization header is missing.");
        }

        var token = authorizationHeader.Split(' ')[1];

        try
        {
            await userSignUpService.ProcessUserSignUpAsync(token);
            return StatusCode(StatusCodes.Status201Created, "User sign-up processed successfully.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Unauthorized. The provided token is invalid or expired.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error. An error occurred while processing the sign-up.");
        }
    }
}
