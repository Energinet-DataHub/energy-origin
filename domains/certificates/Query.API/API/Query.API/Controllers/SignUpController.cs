using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.AspNetCore;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class SignUpController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(302)]
    [Route("api/signup")]
    public async Task<ActionResult> SignUp([FromServices] IQuerySession querySession, [FromBody] CreateSignup createSignup, [FromServices] IValidator<CreateSignup> validator)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        // Validate CreateSignup
        var validationResult = await validator.ValidateAsync(createSignup);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        // Check ownership and if it is production type of GSRN in datahub

        // Check if GSRN is already signed up
        // Save

        return Ok();
    }
}

public record CreateSignup(string Gsrn, long StartDate);
//TODO: Can GSRN be a long or other type?
//TODO: How does datasyncsyncer handle start times not on an even hour?
