using System.Security.Claims;
using API.Query.API.ApiModels.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class UuidController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(OwnerUuid), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [Route("api/certificates/owner/uuid")]
    public IActionResult GetUuid()
    {
        var subject = User.FindFirstValue("subject");

        return string.IsNullOrWhiteSpace(subject)
            ? NotFound()
            : Ok(new OwnerUuid { UUID = subject });
    }
}
