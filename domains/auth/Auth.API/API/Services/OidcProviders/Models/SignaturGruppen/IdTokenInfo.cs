using System.Text.Json.Serialization;

namespace API.Services.OidcProviders.Models.SignaturGruppen;
#nullable disable

public record IdTokenInfo(
    string Iss,
    int Nbf,
    int Iat,
    int Exp,
    string Aud,
    List<string> Amr,
    [property: JsonPropertyName("at_hash")] string AtHash,
    string Sub,
    [property: JsonPropertyName("auth_time")] int AuthTime,
    string Idp,
    string Acr,
    [property: JsonPropertyName("neb_sid")] string NebSid,
    string Loa,
    string Aal,
    string Ial,
    [property: JsonPropertyName("identity_type")] string IdentityType,
    [property: JsonPropertyName("transaction_id")] string TransactionId,
    [property: JsonPropertyName("idp_transaction_id")] string IdpTransactionId,
    [property: JsonPropertyName("session_expiry")] string SessionExpiry
    );
