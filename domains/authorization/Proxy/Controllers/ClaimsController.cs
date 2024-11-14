using System.ComponentModel;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Proxy.Controllers;

[ApiController]
[Route("wallet-api")]
public class ClaimsController : ProxyBase
{

    public ClaimsController(IHttpClientFactory httpClientFactory, IHttpContextAccessor? httpContextAccessor) : base(httpClientFactory, httpContextAccessor)
    {
    }

    /// <summary>
    /// Gets all claims in the wallet
    /// </summary>
    /// <response code="200">Returns all the indiviual claims.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("claims/cursor")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<Claim, PageInfoCursor>), StatusCodes.Status200OK)]
    public async Task GetClaimsCursor([FromQuery] GetClaimsQueryParametersCursor param, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/claims/cursor", organizationId);
    }

    /// <summary>
    /// Gets all claims in the wallet
    /// </summary>
    /// <response code="200">Returns all the indiviual claims.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("v1/claims")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<Claim, PageInfo>), StatusCodes.Status200OK)]
    public async Task GetClaimsLegacy([FromQuery] GetClaimsQueryParameters param)
    {
        await ProxyTokenValidationRequest("v1/claims");
    }

    /// <summary>
    /// Gets all claims in the wallet
    /// </summary>
    /// <response code="200">Returns all the indiviual claims.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("claims")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<Claim, PageInfo>), StatusCodes.Status200OK)]
    public async Task GetClaims([FromQuery] GetClaimsQueryParameters param, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/claims", organizationId);
    }

    /// <summary>
    /// Returns a list of aggregates claims for the authenticated user based on the specified time zone and time range.
    /// </summary>
    /// <response code="200">Returns the aggregated claims.</response>
    /// <response code="400">If the time zone is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("v1/aggregate-claims")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResultList<AggregatedClaims, PageInfo>), StatusCodes.Status200OK)]
    public async Task AggregateClaimsLegacy([FromQuery] AggregateClaimsQueryParameters param)
    {
        await ProxyTokenValidationRequest("v1/aggregate-claims");
    }

    /// <summary>
    /// Returns a list of aggregates claims for the authenticated user based on the specified time zone and time range.
    /// </summary>
    /// <response code="200">Returns the aggregated claims.</response>
    /// <response code="400">If the time zone is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("aggregate-claims")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResultList<AggregatedClaims, PageInfo>), StatusCodes.Status200OK)]
    public async Task AggregateClaims([FromQuery] AggregateClaimsQueryParameters param, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/aggregate-claims", organizationId);
    }

    /// <summary>
    /// Queues a request to claim two certificate for a given quantity.
    /// </summary>
    /// <param name="request">The claim request</param>
    /// <response code="202">Claim request has been queued for processing.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost]
    [Route("v1/claims")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status202Accepted)]
    public async Task ClaimCertificateLegacy([FromBody] ClaimRequest request)
    {
        await ProxyTokenValidationRequest("v1/claims");
    }

    /// <summary>
    /// Queues a request to claim two certificate for a given quantity.
    /// </summary>
    /// <param name="request">The claim request</param>
    /// <response code="202">Claim request has been queued for processing.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost]
    [Route("claims")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status202Accepted)]
    public async Task ClaimCertificate([FromBody] ClaimRequest request, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/claims", organizationId);
    }
}


public record GetClaimsQueryParameters
{
    /// <summary>
    /// The start of the time range in Unix time in seconds.
    /// </summary>
    public long? Start { get; init; }

    /// <summary>
    /// The end of the time range in Unix time in seconds.
    /// </summary>
    public long? End { get; init; }

    /// <summary>
    /// The number of items to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The number of items to skip.
    /// </summary>
    [DefaultValue(0)]
    public int Skip { get; init; }
}

public record AggregateClaimsQueryParameters
{
    /// <summary>
    /// The size of each bucket in the aggregation
    /// </summary>
    public required TimeAggregate TimeAggregate { get; init; }

    /// <summary>
    /// The time zone. See https://en.wikipedia.org/wiki/List_of_tz_database_time_zones for a list of valid time zones.
    /// </summary>
    public required string TimeZone { get; init; }

    /// <summary>
    /// The start of the time range in Unix time in seconds.
    /// </summary>
    public long? Start { get; init; }

    /// <summary>
    /// The end of the time range in Unix time in seconds.
    /// </summary>
    public long? End { get; init; }

    /// <summary>
    /// The number of items to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The number of items to skip.
    /// </summary>
    [DefaultValue(0)]
    public int Skip { get; init; }
}

/// <summary>
/// A claim record representing a claim of a production and consumption certificate.
/// </summary>
public record Claim()
{
    public required Guid ClaimId { get; init; }
    public required uint Quantity { get; init; }
    public required ClaimedCertificate ProductionCertificate { get; init; }
    public required ClaimedCertificate ConsumptionCertificate { get; init; }
}


/// <summary>
/// Info record of a claimed certificate.
/// </summary>
public record ClaimedCertificate()
{
    /// <summary>
    /// The id of the claimed certificate.
    /// </summary>
    public required FederatedStreamId FederatedStreamId { get; init; }

    /// <summary>
    /// The start period of the claimed certificate.
    /// </summary>
    public required long Start { get; init; }

    /// <summary>
    /// The end period the claimed certificate.
    /// </summary>
    public required long End { get; init; }

    /// <summary>
    /// The Grid Area of the claimed certificate.
    /// </summary>
    public required string GridArea { get; init; }

    /// <summary>
    /// The attributes of the claimed certificate.
    /// </summary>
    public required Dictionary<string, string> Attributes { get; init; }
}

/// <summary>
/// A request to claim a production and consumption certificate.
/// </summary>
public record ClaimRequest()
{
    /// <summary>
    /// The id of the production certificate to claim.
    /// </summary>
    public required FederatedStreamId ProductionCertificateId { get; init; }

    /// <summary>
    /// The id of the consumption certificate to claim.
    /// </summary>
    public required FederatedStreamId ConsumptionCertificateId { get; init; }

    /// <summary>
    /// The quantity of the certificates to claim.
    /// </summary>
    public required uint Quantity { get; init; }
}

/// <summary>
/// A response to a claim request.
/// </summary>
public record ClaimResponse()
{
    /// <summary>
    /// The id of the claim request.
    /// </summary>
    public required Guid ClaimRequestId { get; init; }
}

/// <summary>
/// A result of aggregated claims.
/// </summary>
public record AggregatedClaims()
{
    /// <summary>
    /// The start of the aggregated period.
    /// </summary>
    public required long Start { get; init; }

    /// <summary>
    /// The end of the aggregated period.
    /// </summary>
    public required long End { get; init; }

    /// <summary>
    /// The quantity of the aggregated claims.
    /// </summary>
    public required long Quantity { get; init; }
}

