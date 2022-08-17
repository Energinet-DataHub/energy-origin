using System.Text.Json.Serialization;

namespace API.Models;

public record OidcToken
{
    [JsonPropertyName("id_token")]
    public string idToken { get; init; }

    [JsonPropertyName("access_token")]
    public string accessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public string expiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public string tokenType { get; init; }

    [JsonPropertyName("scope")]
    public string scope { get; init; }
}
