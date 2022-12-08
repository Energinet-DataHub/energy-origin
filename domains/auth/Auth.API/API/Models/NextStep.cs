using System.Text.Json.Serialization;

namespace API.Models;
#nullable disable
public record NextStep
{
    [JsonPropertyName("next_url")]
    public string NextUrl { get; init; }
}
