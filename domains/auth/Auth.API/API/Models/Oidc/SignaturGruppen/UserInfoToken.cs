using System.Text.Json.Serialization;

namespace API.Models;
#nullable disable
public record UserInfoToken
{
    [JsonPropertyName("iss")]
    public string Iss { get; init; }

    [JsonPropertyName("nbf")]
    public int Nbf { get; init; }

    [JsonPropertyName("iat")]
    public int Iat { get; init; }

    [JsonPropertyName("exp")]
    public int Exp { get; init; }

    [JsonPropertyName("amr")]
    public List<string> Amr { get; init; }

    [JsonPropertyName("idp")]
    public string Idp { get; init; }

    [JsonPropertyName("nemid.ssn")]
    public string NemidSsn { get; init; }

    [JsonPropertyName("nemid.common_name")]
    public string NemidCommonName { get; init; }

    [JsonPropertyName("nemid.dn")]
    public string NemidDn { get; init; }

    [JsonPropertyName("nemid.rid")]
    public string NemidRid { get; init; }

    [JsonPropertyName("nemid.company_name")]
    public string NemidCompanyName { get; init; }

    [JsonPropertyName("nemid.cvr")]
    public string NemidCvr { get; init; }

    [JsonPropertyName("identity_type")]
    public string IdentityType { get; init; }

    [JsonPropertyName("auth_time")]
    public string AuthTime { get; init; }

    [JsonPropertyName("sub")]
    public string Sub { get; init; }

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; init; }

    [JsonPropertyName("aud")]
    public string Aud { get; init; }

    public bool IsPrivate => IdentityType == "private";

}
