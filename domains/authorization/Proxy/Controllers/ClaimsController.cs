using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Proxy.Controllers;

namespace Proxy.Controllers;

[ApiController]
[Route("wallet-api")]
[SwaggerTag("In the claim process, both quantity and time are taken into account - and if the entire production certificate is not 'used', a new 'slice' is created for the remaining quantity.")]

public class ClaimsController : ProxyBase
{
    private readonly IdentityDescriptor _identityDescriptor;

    public ClaimsController(IHttpClientFactory httpClientFactory, IHttpContextAccessor? httpContextAccessor, IdentityDescriptor identityDescriptor) : base(httpClientFactory, httpContextAccessor)
    {
        _identityDescriptor = identityDescriptor;
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
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<Claim, PageInfoCursor>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetClaimsCursor([FromQuery] GetClaimsQueryParametersCursor param, [Required][FromQuery] string organizationId)
    {
        return ProxyClientCredentialsRequest("v1/claims/cursor", organizationId);
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
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<Claim, PageInfo>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetClaims([FromQuery] GetClaimsQueryParameters param, [Required][FromQuery] string organizationId)
    {
        return ProxyClientCredentialsRequest("v1/claims", organizationId);
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
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResultList<AggregatedClaims, PageInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AggregateClaims([FromQuery] AggregateClaimsQueryParameters param, [Required][FromQuery] string organizationId)
    {
        var isTrial = _identityDescriptor.IsTrial();
        var trialFilter = isTrial ? TrialFilter.Trial : TrialFilter.NonTrial;
        var customQueryString = HttpContext.Request.QueryString.ToString() + $"&TrialFilter={trialFilter}";

        return await ProxyClientCredentialsRequest("v1/aggregate-claims", organizationId, customQueryString);
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
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status202Accepted)]
    public Task<IActionResult> ClaimCertificate([FromBody] ClaimRequest request, [Required][FromQuery] string organizationId)
    {
        return ProxyClientCredentialsRequest("v1/claims", organizationId);
    }
}

public enum TimeMatch
{
    Hourly,
    All,
}

public enum TrialFilter
{
    NonTrial,
    Trial,
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

    /// <summary>
    /// Fetch all or hourly claims.
    /// Values:
    /// All,
    /// Hourly (default: Hourly)
    /// </summary>
    [DefaultValue(TimeMatch.Hourly)]

    public TimeMatch TimeMatch { get; init; } = TimeMatch.Hourly;
}

public record AggregateClaimsQueryParameters
{
    /// <summary>
    /// The size of each bucket in the aggregation
    /// </summary>
    [Required]
    public required TimeAggregate TimeAggregate { get; init; }

    /// <summary>
    /// The time zone. See https://en.wikipedia.org/wiki/List_of_tz_database_time_zones for a list of valid time zones.
    /// </summary>
    [Required]
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

    /// <summary>
    /// Filter for trial or non-trial certificates. Values: NonTrial (default), Trial
    /// </summary>
    [DefaultValue(TrialFilter.NonTrial)]
    public TrialFilter TrialFilter { get; init; } = TrialFilter.NonTrial;
}

/// <summary>
/// A claim record representing a claim of a production and consumption certificate.
/// </summary>
public record Claim()
{
    [Required]
    public required Guid ClaimId { get; init; }
    [Required]
    public required uint Quantity { get; init; }
    [Required]
    public required ClaimedCertificate ProductionCertificate { get; init; }
    [Required]
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
    [Required]
    public required FederatedStreamId FederatedStreamId { get; init; }

    /// <summary>
    /// The start period of the claimed certificate.
    /// </summary>
    [Required]
    public required long Start { get; init; }

    /// <summary>
    /// The end period the claimed certificate.
    /// </summary>
    [Required]
    public required long End { get; init; }

    /// <summary>
    /// The Grid Area of the claimed certificate.
    /// </summary>
    [Required]
    public required string GridArea { get; init; }

    /// <summary>
    /// The attributes of the claimed certificate.
    /// </summary>
    [Required]
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
    [Required]
    public required FederatedStreamId ProductionCertificateId { get; init; }

    /// <summary>
    /// The id of the consumption certificate to claim.
    /// </summary>
    [Required]
    public required FederatedStreamId ConsumptionCertificateId { get; init; }

    /// <summary>
    /// The quantity of the certificates to claim.
    /// </summary>
    [Required]
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
    [Required]
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
    [Required]
    public required long Start { get; init; }

    /// <summary>
    /// The end of the aggregated period.
    /// </summary>
    [Required]
    public required long End { get; init; }

    /// <summary>
    /// The quantity of the aggregated claims.
    /// </summary>
    [Required]
    public required long Quantity { get; init; }
}

