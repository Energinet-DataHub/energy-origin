using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Proxy.Controllers;

[ApiController]
[Route("wallet-api")]
public class RequestStatusController : ProxyBase
{
    public RequestStatusController(IHttpClientFactory httpClientFactory, IHttpContextAccessor? httpContextAccessor)
        : base(httpClientFactory, httpContextAccessor)
    {
    }

    /// <summary>
    /// Get status of claim or transfer request.
    /// </summary>
    /// <remarks>
    /// This endpoint is used to get the status of a claim or transfer request.
    /// These are asynchronous operations and the status will be updated as the request is processed.
    /// </remarks>
    /// <response code="200">The status was successfully found.</response>
    /// <response code="400">The user is not authenticated.</response>
    /// <response code="404">Status not found.</response>
    [HttpGet]
    [Route("request-status/{requestId}")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ProducesResponseType(typeof(RequestStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetRequestStatus([FromRoute] Guid requestId, [Required][FromQuery] string organizationId)
    {
        return ProxyClientCredentialsRequest($"v1/request-status/{requestId}", organizationId);
    }
}

/// <summary>
/// Request status response.
/// </summary>
public record RequestStatusResponse()
{
    /// <summary>
    /// The status of the request.
    /// </summary>
    public required RequestStatus Status { get; init; }
}

public enum RequestStatus { Pending, Completed, Failed }
