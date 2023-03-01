using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class SubjectController : ControllerBase
{
    [HttpGet]
    [Route("api/certificates/subject")]
    public IActionResult GetUuid() => Ok(User.FindFirstValue("subject"));
}
