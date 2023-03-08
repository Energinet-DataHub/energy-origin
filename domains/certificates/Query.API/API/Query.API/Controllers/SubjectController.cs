using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class SubjectController : ControllerBase
{
    /// <summary>
    /// Endpoint to return the Subject UUID. Will be removed from the API in the future
    /// </summary>
    [HttpGet]
    [Route("api/certificates/subject")]
    public IActionResult GetSubject() => Ok(User.FindFirstValue("subject"));
}
