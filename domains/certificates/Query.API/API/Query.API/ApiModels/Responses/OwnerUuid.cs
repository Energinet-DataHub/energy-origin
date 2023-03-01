using System;
using System.Text.Json.Serialization;

namespace API.Query.API.ApiModels.Responses;

public record OwnerUuid
{
    [JsonPropertyName("uuid")]
    public string UUID { get; init; }

}
