using System.Text.Json.Serialization;

namespace API.Models;
#nullable disable
public record SignaturGruppenMitId
{
    [JsonPropertyName("identity_type")]
    public string IdentityType { get; init; }

    [JsonPropertyName("idp_identity_id")]
    public string IdpIdentityId { get; init; }

    [JsonPropertyName("loa")]
    public string Loa { get; init; }

    [JsonPropertyName("aal")]
    public string Aal { get; init; }

    [JsonPropertyName("ial")]
    public string Ial { get; init; }

    [JsonPropertyName("mitid.uuid")]
    public string Uuid { get; init; }

    [JsonPropertyName("mitid.age")]
    public string Age { get; init; }

    [JsonPropertyName("mitid.date_of_birth")]
    public string Birth { get; init; }

    [JsonPropertyName("mitid.identity_name")]
    public string Name { get; init; }

    [JsonPropertyName("mitid.transaction_id")]
    public string TransactionId { get; init; }

    [JsonPropertyName("sub")]
    public string Sub { get; init; }
}


