using System;

namespace API.Query.API.ApiModels.Requests;

public class TransferCertificate
{
    public string CurrentOwner { get; init; } = "";

    public string NewOwner { get; init; } = "";

    public Guid CertificateId { get; init; }
}
