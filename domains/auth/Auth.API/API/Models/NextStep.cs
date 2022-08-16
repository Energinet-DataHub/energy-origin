using System.Text.Json.Serialization;

namespace API.Models;

public record NextStep
{
    [JsonPropertyName("next_url")]
    public string NextUrl { get; init; }
}
