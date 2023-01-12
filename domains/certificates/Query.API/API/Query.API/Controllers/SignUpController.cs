using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
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
        Guid.Parse(meteringPointOwner);

        await validator.ValidateAsync(createSignup);

        //var exists = querySession.Events.QueryAllRawEvents()
        //    .Where(x => x.StreamId == Guid.Parse(meteringPointOwner))
        //    .OrderBy(x => x.Sequence)
        //    .ToList();

        return Ok();
    }
}

public record CreateSignup(string Gsrn, long StartDate);
//TODO: Can GSRN be a long or other type?
//TODO: How does datasyncsyncer handle start times not on an even hour?
