using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Proxy.Controllers;

[ApiController]
[Route("wallet-api")]

public class TransfersController : ProxyBase
{
    public TransfersController(IHttpClientFactory httpClientFactory, IHttpContextAccessor? httpContextAccessor) : base(httpClientFactory, httpContextAccessor)
    {
    }

    /// <summary>
    /// Gets detailed list of all of the transfers that have been made to other wallets.
    /// </summary>
    /// <response code="200">Returns the individual transferes within the filter.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("transfers/cursor")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<Transfer, PageInfoCursor>), StatusCodes.Status200OK)]
    public async Task GetTransfersCursor([FromQuery] GetTransfersQueryParametersCursor param, [Required][FromQuery] string organizationId)
    {
        await ProxyClientCredentialsRequest("v1/transfers/cursor", organizationId);
    }

    /// <summary>
    /// Gets detailed list of all the transfers that have been made to other wallets.
    /// </summary>
    /// <response code="200">Returns the individual transferes within the filter.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("transfers")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResultList<Transfer, PageInfo>), StatusCodes.Status200OK)]
    public async Task GetTransfers([FromQuery] GetTransfersQueryParameters param, [Required][FromQuery] string organizationId)
    {
        await ProxyClientCredentialsRequest("v1/transfers", organizationId);
    }

    /// <summary>
    /// Gets detailed list of all the transfers that have been made to other wallets.
    /// </summary>
    /// <response code="200">Returns the individual transferes within the filter.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("aggregate-transfers")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResultList<AggregatedTransfers, PageInfo>), StatusCodes.Status200OK)]
    public async Task AggregateTransfers([FromQuery] AggregateTransfersQueryParameters param, [Required][FromQuery] string organizationId)
    {
        await ProxyClientCredentialsRequest("v1/aggregate-transfers", organizationId);
    }

    /// <summary>
    /// Transfers a certificate to another wallet.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="organizationId"></param>
    [HttpPost]
    [Route("transfers")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    public async Task TransferCertificate([FromBody] TransferRequest request, [Required][FromQuery] string organizationId)
    {
        await ProxyClientCredentialsRequest("v1/transfers", organizationId);
    }
}


public record GetTransfersQueryParameters
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

public record AggregateTransfersQueryParameters
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
}

/// <summary>
/// A transfer record of a transfer of a part of a certificate to another wallet.
/// </summary>
public record Transfer()
{
    [Required]
    public required FederatedStreamId FederatedStreamId { get; init; }
    [Required]
    public required string ReceiverId { get; init; }
    [Required]
    public required long Quantity { get; init; }
    [Required]
    public required long Start { get; init; }
    [Required]
    public required long End { get; init; }
    [Required]
    public required string GridArea { get; init; }
}

/// <summary>
/// A request to transfer a certificate to another wallet.
/// </summary>
public record TransferRequest()
{
    /// <summary>
    /// The federated stream id of the certificate to transfer.
    /// </summary>
    [Required]
    public required FederatedStreamId CertificateId { get; init; }

    /// <summary>
    /// The id of the wallet to transfer the certificate to.
    /// </summary>
    [Required]
    public required Guid ReceiverId { get; init; }

    /// <summary>
    /// The quantity of the certificate to transfer.
    /// </summary>
    [Required]
    public required uint Quantity { get; init; }

    /// <summary>
    /// List of hashed attributes names to transfer with the certificate.
    /// Can be empty array if no hashed attributes are to be transferred.
    /// </summary>
    [Required]
    public required string[] HashedAttributes { get; init; }
}

/// <summary>
/// A response to a transfer request.
/// </summary>
public record TransferResponse()
{
    /// <summary>
    /// The id of the transfer request.
    /// </summary>
    [Required]
    public required Guid TransferRequestId { get; init; }
}

/// <summary>
/// A result of aggregated transfers.
/// </summary>
public record AggregatedTransfers()
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
    /// The quantity of the aggregated transfers.
    /// </summary>
    [Required]
    public required long Quantity { get; init; }
}
