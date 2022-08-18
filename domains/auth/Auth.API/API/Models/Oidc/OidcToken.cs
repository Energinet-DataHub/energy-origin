using System.Text.Json.Serialization;

namespace API.Models.Oidc;

public record OidcToken
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; init; }

    [JsonPropertyName("redirect_uri")]
    public string RedirectUrl { get; init; }

    [JsonPropertyName("code")]
    public string Code { get; init; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; }

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; init; }


}
