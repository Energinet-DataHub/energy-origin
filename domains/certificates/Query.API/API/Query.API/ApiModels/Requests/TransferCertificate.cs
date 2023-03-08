using System;

namespace API.Query.API.ApiModels.Requests;

public class TransferCertificate
{
    /// <summary>
    /// Id of the certificate
    /// </summary>
    public Guid CertificateId { get; init; }

    /// <summary>
    /// Key for the current owner of the certificate
    /// </summary>
    public string Source { get; init; } = "";

    /// <summary>
    /// Key to transfer the certificate to
    /// </summary>
    public string Target { get; init; } = "";
}
