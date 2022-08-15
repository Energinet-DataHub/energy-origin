using System.Text.Json.Serialization;

namespace API.Models;

public record OidcCallback
{
    [JsonPropertyName("state")]
    public string state { get; init; }

    [JsonPropertyName("iss")]
    public string iss { get; init; }

    [JsonPropertyName("code")]
    public string code { get; init; }

    [JsonPropertyName("scope")]
    public string scope { get; init; }

    [JsonPropertyName("error")]
    public string error { get; init; }

    [JsonPropertyName("error_hint")]
    public string errorHint { get; init; }

    [JsonPropertyName("error_description")]
    public string errorDescription { get; init; }

}
