using System;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20230101)]
[Route("api/authorization/access-control")]
public class AccessControlController : ControllerBase
{
    /// <summary>
    /// Determines whether the request is authenticated and authorized.
    /// </summary>
    /// <param name="organizationId">The organization ID to check against the token's claims. Must be a valid GUID.</param>
    /// <returns>
    /// A status code indicating the result of the authorization check.
    /// </returns>
    /// <response code="200">Success</response>
    /// <response code="400">Invalid organizationId</response>
    /// <response code="401">Unauthenticated</response>
    /// <response code="403">Unauthorized</response>
    [HttpGet]
    [Authorize(Policy = Policy.B2CPolicy)]
    public IActionResult Decision([FromQuery] Guid organizationId)
    {
        var identity = new IdentityDescriptor(HttpContext, organizationId);

        if (organizationId == Guid.Empty)
            return ValidationProblem("Must provide a valid organizationId as a GUID");

        Response.Headers.Append("x-wallet-owner", organizationId.ToString());
        return Ok();
    }
}
