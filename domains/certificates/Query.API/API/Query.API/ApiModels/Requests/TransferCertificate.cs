using System;

namespace API.Query.API.ApiModels.Requests;

public class TransferCertificate
{
    /// <summary>
    /// Id of the certificate
    /// </summary>
    public Guid CertificateId { get; init; }

    /// <summary>
    /// Key for the owner currently owning the certificate
    /// </summary>
    public string CurrentOwner { get; init; } = ""; //TODO: Do we want to call this Current and New? It is not consistent with the rest of the code?

    /// <summary>
    /// Key for the owner to transfer the certificate to
    /// </summary>
    public string NewOwner { get; init; } = "";
}
