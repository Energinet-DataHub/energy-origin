using System.Text.Json.Serialization;

namespace API.Models;

public record OidcTokenResponse
{
    [JsonPropertyName("id_token")]
    public string IdToken { get; init; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public string ExpiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; }

    [JsonPropertyName("scope")]
    public string Scope { get; init; }
}
