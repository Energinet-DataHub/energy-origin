using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Marten;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class SignUpController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(302)]
    [Route("api/signup")]
    public async Task<ActionResult> SignUp([FromServices] IQuerySession querySession)
    {
        var meteringPointOwner = User.FindFirstValue("subject");
        Guid.Parse(meteringPointOwner);

        var exists = querySession.Events.QueryAllRawEvents()
            .Where(x => x.StreamId == Guid.Parse(meteringPointOwner))
            .OrderBy(x => x.Sequence)
            .ToList();


    }


}
