using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;

namespace Proxy.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20250101)]
public class CertificatesController : ProxyBase
{
    public CertificatesController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet]
    [Route("certificates")]
    [Produces("application/json")]
    [Authorize(policy: Policy.B2CPolicy)]
    [ApiVersion(ApiVersions.Version20250101)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<GranularCertificate>), StatusCodes.Status200OK)]
    public async Task GetCertificatesV2([FromQuery] GetCertificatesQueryParameters param, [FromQuery]string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/certificates", organizationId);
    }

    [HttpGet]
    [Route("v1/certificates")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersion(ApiVersions.Version20250101)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<GranularCertificate>), StatusCodes.Status200OK)]
    public async Task GetCertificates([FromQuery] GetCertificatesQueryParameters param)
    {
        await ProxyTokenValidationRequest("v1/certificates");
    }

    [HttpGet]
    [Route("aggregate-certificates")]
    [Produces("application/json")]
    [Authorize(policy: Policy.B2CPolicy)]
    [ApiVersion(ApiVersions.Version20250101)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<AggregatedCertificates>), StatusCodes.Status200OK)]
    public async Task AggregateCertificatesV2([FromQuery] AggregateCertificatesQueryParameters param, [FromQuery]string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/aggregate-certificates", organizationId);
    }

    [HttpGet]
    [Route("v1/aggregate-certificates")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersion(ApiVersions.Version20250101)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<AggregatedCertificates>), StatusCodes.Status200OK)]
    public async Task AggregateCertificates([FromQuery] AggregateCertificatesQueryParameters param)
    {
        await ProxyTokenValidationRequest("v1/aggregate-certificates");
    }

}
