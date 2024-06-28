using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Proxy.Controllers;

[ApiController]
public class RequestStatusController : ProxyBase
{
    public RequestStatusController(IHttpClientFactory httpClientFactory) : base(httpClientFactory, null)
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
    [Route("v1/request-status/{requestId}")]
    [Produces("application/json")]
    [Authorize(policy: Policy.B2CPolicy)]
    [ApiVersion(ApiVersions.Version20250101)]
    [ProducesResponseType(typeof(ReceiveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task GetRequestStatus([FromRoute] Guid requestId, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest($"v1/request-status/{requestId}", organizationId);
    }
}
