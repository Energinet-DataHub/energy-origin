using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Proxy.Controllers;

[ApiController]
[Route("wallet-api")]
public class WalletController : ProxyBase
{
    public WalletController(IHttpClientFactory httpClientFactory, IHttpContextAccessor? httpContextAccessor) : base(httpClientFactory, httpContextAccessor)
    {
    }

    /// <summary>
    /// Creates a new wallet for the user.
    /// </summary>
    /// <remarks>
    /// Currently, only **one wallet** per user is supported.
    /// The wallet is created with a private key, which is used to generate sub-public-keys for each certificate-slice.
    /// The private key can be provided, but it is optional, if omittted a random one is generated.
    /// </remarks>
    /// <param name = "request" > The private key to import. If not provided, a new private key will be generated.</param>
    /// <response code="201">The wallet was created.</response>
    /// <response code="400">If private key is invalid or if wallet for user already exists.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost]
    [Route("v1/wallets")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(CreateWalletResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task CreateWalletLegacy([FromBody] CreateWalletRequest request)
    {
        await ProxyTokenValidationRequest("v1/wallets");
    }

    /// <summary>
    /// Creates a new wallet for the user.
    /// </summary>
    /// <remarks>
    /// Currently, only **one wallet** per user is supported.
    /// The wallet is created with a private key, which is used to generate sub-public-keys for each certificate-slice.
    /// The private key can be provided, but it is optional, if omittted a random one is generated.
    /// </remarks>
    /// <response code="201">The wallet was created.</response>
    /// <response code="400">If private key is invalid or if wallet for user already exists.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost]
    [Route("wallets")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(CreateWalletResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task CreateWallet([FromBody] CreateWalletRequest request, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/wallets", organizationId);
    }

    /// <summary>
    /// Gets all wallets for the user.
    /// </summary>
    /// <response code="200">The wallets were found.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("v1/wallets")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(ResultList<WalletRecord, PageInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task GetWalletsLegacy()
    {
        await ProxyTokenValidationRequest("v1/wallets");
    }

    /// <summary>
    /// Gets all wallets for the user.
    /// </summary>
    /// <response code="200">The wallets were found.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [Route("wallets")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(ResultList<WalletRecord, PageInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task GetWallets([FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/wallets", organizationId);
    }

    /// <summary>
    /// Gets a specific wallet for the user.
    /// </summary>
    /// <param name="walletId">The ID of the wallet to get.</param>
    /// <response code="200">The wallet was found.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the wallet specified is not found for the user.</response>
    [HttpGet]
    [Route("v1/wallets/{walletId}")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(WalletRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task GetWalletLegacy([FromRoute] Guid walletId)
    {
        await ProxyTokenValidationRequest($"v1/wallets/{walletId}");
    }

    /// <summary>
    /// Gets a specific wallet for the user.
    /// </summary>
    /// <param name="walletId">The ID of the wallet to get.</param>
    /// <response code="200">The wallet was found.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the wallet specified is not found for the user.</response>
    [HttpGet]
    [Route("wallets/{walletId}")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(WalletRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task GetWallet([FromRoute] Guid walletId, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest($"v1/wallets/{walletId}", organizationId);
    }

    [HttpPost]
    [Route("v1/wallets/{walletId}/endpoints")]
    [Produces("application/json")]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(WalletRecord), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task CreateWalletEndpointLegacy([FromRoute] Guid walletId)
    {
        await ProxyTokenValidationRequest($"v1/wallets/{walletId}/endpoints");
    }

    [HttpPost]
    [Route("wallets/{walletId}/endpoints")]
    [Produces("application/json")]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(WalletRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task CreateWalletEndpoint([FromRoute] Guid walletId, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest($"v1/wallets/{walletId}/endpoints", organizationId);
    }

    [HttpPost]
    [Route("v1/external-endpoints")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreateExternalEndpointResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task CreateExternalEndpointLegacy([FromBody] CreateExternalEndpointRequest request)
    {
        await ProxyTokenValidationRequest("v1/external-endpoints");
    }

    [HttpPost]
    [Route("external-endpoints")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreateExternalEndpointResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(policy: Policy.FrontendOr3rdParty)]
    [ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task CreateExternalEndpoint([FromBody] CreateExternalEndpointRequest request, [FromQuery] string? organizationId)
    {
        await ProxyClientCredentialsRequest("v1/external-endpoints", organizationId);
    }
}


/// <summary>
/// A wallet record
/// </summary>
public record WalletRecord()
{
    public required Guid Id { get; init; }
    public required string PublicKey { get; init; }
}

/// <summary>
/// Request to create a new wallet.
/// </summary>
public record CreateWalletRequest()
{
    /// <summary>
    /// The private key to import. If not provided, a private key will be generated.
    /// </summary>
    public byte[]? PrivateKey { get; init; }
}

/// <summary>
/// Response to create a new wallet.
/// </summary>
public record CreateWalletResponse()
{
    /// <summary>
    /// The ID of the created wallet.
    /// </summary>
    public Guid WalletId { get; init; }
}

/// <summary>
/// Response to create a new wallet endpoint on the users wallet.
/// </summary>
public record CreateWalletEndpointResponse()
{
    /// <summary>
    /// Reference object to the wallet endpoint created.
    /// Contains the necessary information to send to another wallet to create a link so certificates can be transferred.
    /// </summary>
    public required WalletEndpointReference WalletReference { get; init; }
}

/// <summary>
/// Request to create a new external endpoint.
/// </summary>
public record CreateExternalEndpointRequest()
{
    /// <summary>
    /// The wallet reference to the wallet, one wants to create a link to.
    /// </summary>
    public required WalletEndpointReference WalletReference { get; init; }

    /// <summary>
    /// The text reference for the wallet, one wants to create a link to.
    /// </summary>
    public required string TextReference { get; init; }
}

/// <summary>
/// Response to create a new external endpoint.
/// </summary>
public record CreateExternalEndpointResponse()
{
    /// <summary>
    /// The ID of the created external endpoint.
    /// </summary>
    public required Guid ReceiverId { get; init; }
}

public record WalletEndpointReference()
{
    /// <summary>
    /// The version of the ReceiveSlice API.
    /// </summary>
    public required int Version { get; init; } // The version of the Wallet protobuf API.

    /// <summary>
    /// The url endpoint of where the wallet is hosted.
    /// </summary>
    public required Uri Endpoint { get; init; }

    /// <summary>
    /// The public key used to generate sub-public-keys for each slice.
    /// </summary>
    public required string PublicKey { get; init; }
}
