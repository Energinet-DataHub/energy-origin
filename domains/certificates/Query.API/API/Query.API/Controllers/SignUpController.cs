using System;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public async Task<ActionResult> SignUp([FromServices] IQuerySession querySession, [FromBody] CreateSignup createSignup)
    {
        var meteringPointOwner = User.FindFirstValue("subject");
        Guid.Parse(meteringPointOwner);

        //var exists = querySession.Events.QueryAllRawEvents()
        //    .Where(x => x.StreamId == Guid.Parse(meteringPointOwner))
        //    .OrderBy(x => x.Sequence)
        //    .ToList();

        return Ok();
    }
}

public record CreateSignup(string Gsrn, long StartDate);
