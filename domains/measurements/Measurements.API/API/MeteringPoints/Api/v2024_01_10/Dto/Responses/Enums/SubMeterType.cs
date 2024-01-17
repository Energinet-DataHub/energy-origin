using System.Text.Json.Serialization;

namespace API.MeteringPoints.Api.v2024_01_10.Dto.Responses.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubMeterType
{
    Physical,
    Virtual,
    Calculated
}
