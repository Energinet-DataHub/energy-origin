using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;

namespace Proxy.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20250101)]
public class CertificatesController : ProxyBase
{
    public CertificatesController(IHttpClientFactory httpClientFactory, IHttpContextAccessor? httpContextAccessor) : base(httpClientFactory, httpContextAccessor)
    {
    }

    /// <summary>
    /// Gets all certificates in the wallet that are <b>available</b> for use.
    /// </summary>
    /// <response code="200">Returns the aggregated claims.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("certificates/cursor")]
    [Produces("application/json")]
    [Authorize(policy: Policy.B2CPolicy)]
    [ApiVersion(ApiVersions.Version20250101)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<GranularCertificate, PageInfoCursor>), StatusCodes.Status200OK)]
    public async Task GetCertificatesCursor([FromQuery] GetCertificatesQueryParametersCursor param, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/certificates/cursor", organizationId);
    }


    /// <summary>
    /// Gets all certificates in the wallet that are <b>available</b> for use.
    /// </summary>
    /// <response code="200">Returns the aggregated claims.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("certificates")]
    [Produces("application/json")]
    [Authorize(policy: Policy.B2CPolicy)]
    [ApiVersion(ApiVersions.Version20250101)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<GranularCertificate, PageInfo>), StatusCodes.Status200OK)]
    public async Task GetCertificates([FromQuery] GetCertificatesQueryParameters param, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/certificates", organizationId);
    }

    /// <summary>
    /// Gets all certificates in the wallet that are <b>available</b> for use.
    /// </summary>
    /// <response code="200">Returns the aggregated claims.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("v1/certificates")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<GranularCertificate, PageInfo>), StatusCodes.Status200OK)]
    public async Task GetCertificatesLegacy([FromQuery] GetCertificatesQueryParameters param)
    {
        await ProxyTokenValidationRequest("v1/certificates");
    }

    /// <summary>
    /// Returns aggregates certificates that are <b>available</b> to use, based on the specified time zone and time range.
    /// </summary>
    /// <response code="200">Returns the aggregated claims.</response>
    /// <response code="400">If the time zone is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("aggregate-certificates")]
    [Produces("application/json")]
    [Authorize(policy: Policy.B2CPolicy)]
    [ApiVersion(ApiVersions.Version20250101, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<AggregatedCertificates, PageInfo>), StatusCodes.Status200OK)]
    public async Task AggregateCertificates([FromQuery] AggregateCertificatesQueryParameters param, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/aggregate-certificates", organizationId);
    }

    /// <summary>
    /// Returns aggregates certificates that are <b>available</b> to use, based on the specified time zone and time range.
    /// </summary>
    /// <response code="200">Returns the aggregated claims.</response>
    /// <response code="400">If the time zone is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("v1/aggregate-certificates")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<AggregatedCertificates, PageInfo>), StatusCodes.Status200OK)]
    public async Task AggregateCertificatesLegacy([FromQuery] AggregateCertificatesQueryParameters param)
    {
        await ProxyTokenValidationRequest("v1/aggregate-certificates");
    }
}
