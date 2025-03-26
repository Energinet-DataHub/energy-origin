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
public class SlicesController : ProxyBase
{
    public SlicesController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {

    }

    /// <summary>
    /// Receive a certificate-slice from another wallet.
    /// </summary>
    /// <remarks>
    /// This request is used to receive a certificate-slice from another wallet, which is then stored in the local wallet.
    /// The endpoint is verified to exists within the wallet system, otherwise a 404 will be returned.
    /// The endpoint will return 202 Accepted was initial validation has succeeded.
    /// The certificate-slice will further verified with data from the registry in a seperate thread.
    /// </remarks>
    /// <param name = "request" >Contains the data </param>
    /// <response code="202">The slice was accepted.</response>
    /// <response code="400">Public key could not be decoded.</response>
    /// <response code="404">Receiver endpoint not found.</response>
    [HttpPost]
    [Route("v1/slices")]
    [Produces("application/json")]
    [AllowAnonymous]
    [ApiVersionNeutral]
    [Obsolete]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ReceiveResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task ReceiveSlice([FromBody] ReceiveRequest request)
    {
        await ProxyInsecureCall("v1/slices");
    }

    /// <summary>
    /// Receive a certificate-slice from another wallet.
    /// </summary>
    /// <remarks>
    /// This request is used to receive a certificate-slice from another wallet, which is then stored in the local wallet.
    /// The endpoint is verified to exists within the wallet system, otherwise a 404 will be returned.
    /// The endpoint will return 202 Accepted was initial validation has succeeded.
    /// The certificate-slice will further verified with data from the registry in a seperate thread.
    /// This is only used in inter-wallet communication
    /// </remarks>
    /// <param name = "request" >Contains the data </param>
    /// <response code="202">The slice was accepted.</response>
    /// <response code="400">Public key could not be decoded.</response>
    /// <response code="404">Receiver endpoint not found.</response>
    [HttpPost]
    [Route("slices")]
    [Produces("application/json")]
    [AllowAnonymous]
    [ApiVersion(ApiVersions.Version1)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ReceiveResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task ReceiveSliceV2([FromBody] ReceiveRequest request)
    {
        await ProxyInsecureCall("v1/slices");
    }
}


/// <summary>
/// Request to receive a certificate-slice from another wallet.
/// </summary>
public record ReceiveRequest()
{
    /// <summary>
    /// The public key of the receiving wallet.
    /// </summary>
    [Required]
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// The sub-position of the publicKey used on the slice on the registry.
    /// </summary>
    [Required]
    public required uint Position { get; init; }

    /// <summary>
    /// The id of the certificate.
    /// </summary>
    [Required]
    public required FederatedStreamId CertificateId { get; init; }

    /// <summary>
    /// The quantity of the slice.
    /// </summary>
    [Required]
    public required uint Quantity { get; init; }

    /// <summary>
    /// The random R used to generate the pedersen commitment with the quantitiy.
    /// </summary>
    [Required]
    public required byte[] RandomR { get; init; }

    /// <summary>
    /// List of hashed attributes, their values and salts so the receiver can access the data.
    /// </summary>
    [Required]
    public required IEnumerable<HashedAttribute> HashedAttributes { get; init; }
}

/// <summary>
/// Hashed attribute with salt.
/// </summary>
public record HashedAttribute()
{

    /// <summary>
    /// The key of the attribute.
    /// </summary>
    [Required]
    public required string Key { get; init; }

    /// <summary>
    /// The value of the attribute.
    /// </summary>
    [Required]
    public required string Value { get; init; }

    /// <summary>
    /// The salt used to hash the attribute.
    /// </summary>
    [Required]
    public required byte[] Salt { get; init; }
}

/// <summary>
/// Response to receive a certificate-slice from another wallet.
/// </summary>
public record ReceiveResponse();
