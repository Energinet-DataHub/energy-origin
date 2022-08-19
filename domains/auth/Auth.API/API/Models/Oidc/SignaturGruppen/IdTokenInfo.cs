using System.Text.Json.Serialization;

namespace API.Models;

public record IdTokenInfo
{
    [JsonPropertyName("iss")]
    public string Iss { get; init; }

    [JsonPropertyName("npf")]
    public int Nbf { get; init; }

    [JsonPropertyName("iat")]
    public int Iat { get; init; }

    [JsonPropertyName("exp")]
    public int Exp { get; init; }

    [JsonPropertyName("aud")]
    public string Aud { get; init; }

    [JsonPropertyName("amr")]
    public List<string> Amr { get; init; }

    [JsonPropertyName("at_hash")]
    public string AtHash { get; init; }

    [JsonPropertyName("sub")]
    public string Sub { get; init; }

    [JsonPropertyName("auth_time")]
    public int AuthTime { get; init; }

    [JsonPropertyName("idp")]
    public string Idp { get; init; }

    [JsonPropertyName("acr")]
    public string Acr { get; init; }

    [JsonPropertyName("neb_sid")]
    public string NebSid { get; init; }

    [JsonPropertyName("loa")]
    public string Loa { get; init; }

    [JsonPropertyName("Aal")]
    public string Aal { get; init; }

    [JsonPropertyName("ial")]
    public string Ial { get; init; }

    [JsonPropertyName("identity_type")]
    public string IdentityType { get; init; }

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; init; }

    [JsonPropertyName("idp_transaction_id")]
    public string IdpTransactionId { get; init; }

    [JsonPropertyName("session_expiry")]
    public string SessionExpiry { get; init; }
}
