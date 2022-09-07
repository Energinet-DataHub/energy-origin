using System.Text.Json.Serialization;

namespace API.Services.OidcProviders.Models.SignaturGruppen;
#nullable disable

public record IdTokenInfo(
    [property: JsonPropertyName("iss")] string Iss,
    [property: JsonPropertyName("nbf")] int Nbf,
    [property: JsonPropertyName("iat")] int Iat,
    [property: JsonPropertyName("exp")] int Exp,
    [property: JsonPropertyName("aud")] string Aud,
    [property: JsonPropertyName("amr")] List<string> Amr,
    [property: JsonPropertyName("at_hash")] string AtHash,
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("auth_time")] int AuthTime,
    [property: JsonPropertyName("idp")] string Idp,
    [property: JsonPropertyName("acr")] string Acr,
    [property: JsonPropertyName("neb_sid")] string NebSid,
    [property: JsonPropertyName("loa")] string Loa,
    [property: JsonPropertyName("aal")] string Aal,
    [property: JsonPropertyName("ial")] string Ial,
    [property: JsonPropertyName("identity_type")] string IdentityType,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    [property: JsonPropertyName("idp_transaction_id")] string IdpTransactionId,
    [property: JsonPropertyName("session_expiry")] string SessionExpiry
    );
