using System.Text.Json.Serialization;

namespace API.Models;
#nullable disable
public record SignaturGruppenNemId
{

    [JsonPropertyName("iat")]
    public string Iat { get; init; }

    [JsonPropertyName("exp")]
    public string Exp { get; init; }

    [JsonPropertyName("sub")]
    public string Sub { get; init; }

    [JsonPropertyName("idp")]
    public string Idp { get; init; }

    [JsonPropertyName("identity_type")]
    public string IdentityType { get; init; }

    [JsonPropertyName("dk.cpr")]
    public string Cpr { get; init; }

    [JsonPropertyName("nemid.cvr")]
    public string Tin { get; init; }

    [JsonPropertyName("nemid.pid")]
    public string Pid { get; init; }

    [JsonPropertyName("nemid.rid")]
    public string Rid { get; init; }
}
