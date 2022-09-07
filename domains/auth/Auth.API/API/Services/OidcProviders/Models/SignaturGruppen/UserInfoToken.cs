using System.Text.Json.Serialization;

namespace API.Services.OidcProviders.Models.SignaturGruppen;
#nullable disable

public record UserInfoToken
(
    string Iss,
    int Nbf,
    int Iat,
    int Exp,
    List<string> Amr,
    string Idp,
    [property: JsonPropertyName("nemid.ssn")] string NemidSsn,
    [property: JsonPropertyName("nemid.common_name")] string NemidCommonName,
    [property: JsonPropertyName("nemid.dn")] string NemidDn,
    [property: JsonPropertyName("nemid.rid")] string NemidRid,
    [property: JsonPropertyName("nemid.company_name")] string NemidCompanyName,
    [property: JsonPropertyName("nemid.cvr")] string NemidCvr,
    [property: JsonPropertyName("identity_type")] string IdentityType,
    [property: JsonPropertyName("auth_time")] string AuthTime,
    string Sub,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    string Aud
);

public static class UserIsPrivate
{
    public static bool IsPrivate(this UserInfoToken token) => token.IdentityType == "private";
}



public record UserInfoTokenold
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
