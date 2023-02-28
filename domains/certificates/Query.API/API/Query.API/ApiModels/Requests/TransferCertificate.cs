using System;
using System.Text.Json.Serialization;

namespace API.Query.API.ApiModels.Requests;

public class TransferCertificate
{
    [JsonPropertyName("current-owner")]
    public string CurrentOwner { get; init; } = "";

    [JsonPropertyName("new-owner")]
    public string NewOwner { get; init; } = "";

    [JsonPropertyName("certificateid")]
    public Guid CertificateId { get; init; }


}
