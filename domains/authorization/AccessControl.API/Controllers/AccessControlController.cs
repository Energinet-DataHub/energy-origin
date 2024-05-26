using System;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.API.Controllers;

[ApiController]
[Route("api/decision")]
[ApiVersion(ApiVersions.Version20230101)]
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
    [Authorize(Policy = "OrganizationAccess")]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 401)]
    [ProducesResponseType(typeof(void), 403)]
    public IActionResult Decision([FromQuery] Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            return ValidationProblem("Must provide a valid organizationId as a GUID");

        Response.Headers.Append("x-wallet-owner", organizationId.ToString());
        return Ok();
    }
}
