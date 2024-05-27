using System;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers;

[ApiController]
[Authorize(Policy = Policy.B2CPolicy)]
[ApiVersion(ApiVersions.Version20230101)]
public class AccessControlController : ControllerBase
{
    [HttpGet]
    [Route("api/authorization/access-control")]
    public IActionResult Decision([FromQuery] Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            return ValidationProblem("Must provide a valid organizationId as a GUID");

        Response.Headers.Append("x-wallet-owner", organizationId.ToString());
        return Ok();
    }
}
