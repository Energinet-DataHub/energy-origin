using System.Collections.Generic;

namespace ProjectOriginClients.Models;

/// <summary>
/// Request to receive a certificate-slice from another wallet.
/// </summary>
public record ReceiveRequest()
{
    /// <summary>
    /// The public key of the receiving wallet.
    /// </summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// The sub-position of the publicKey used on the slice on the registry.
    /// </summary>
    public required uint Position { get; init; }

    /// <summary>
    /// The id of the certificate.
    /// </summary>
    public required FederatedStreamId CertificateId { get; init; }

    /// <summary>
    /// The quantity of the slice.
    /// </summary>
    public required uint Quantity { get; init; }

    /// <summary>
    /// The random R used to generate the pedersen commitment with the quantity.
    /// </summary>
    public required byte[] RandomR { get; init; }

    /// <summary>
    /// List of hashed attributes, their values and salts so the receiver can access the data.
    /// </summary>
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
    public required string Key { get; init; }

    /// <summary>
    /// The value of the attribute.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// The salt used to hash the attribute.
    /// </summary>
    public required byte[] Salt { get; init; }
}

public record ReceiveResponse() { }
