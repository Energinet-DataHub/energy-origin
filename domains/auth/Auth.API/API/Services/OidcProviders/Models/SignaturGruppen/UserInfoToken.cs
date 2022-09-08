using System.Text.Json.Serialization;

namespace API.Services.OidcProviders.Models.SignaturGruppen;
#nullable disable

public record UserInfoToken
(
    [property: JsonPropertyName("iss")] string Iss,
    [property: JsonPropertyName("nbf")] int Nbf,
    [property: JsonPropertyName("iat")] int Iat,
    [property: JsonPropertyName("exp")] int Exp,
    [property: JsonPropertyName("amr")] List<string> Amr,
    [property: JsonPropertyName("idp")] string Idp,
    [property: JsonPropertyName("nemid.ssn")] string NemidSsn,
    [property: JsonPropertyName("nemid.common_name")] string NemidCommonName,
    [property: JsonPropertyName("nemid.dn")] string NemidDn,
    [property: JsonPropertyName("nemid.rid")] string NemidRid,
    [property: JsonPropertyName("nemid.company_name")] string NemidCompanyName,
    [property: JsonPropertyName("nemid.cvr")] string NemidCvr,
    [property: JsonPropertyName("identity_type")] string IdentityType,
    [property: JsonPropertyName("auth_time")] string AuthTime,
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    [property: JsonPropertyName("aud")] string Aud
);

public static class UserIsPrivate
{
    public static bool IsPrivate(this UserInfoToken token) => token.IdentityType == "private";
}
