using System.Text.Json.Serialization;

namespace API.MeteringPoints.Api.Dto.Responses.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubMeterType
{
    Physical,
    Virtual,
    Calculated
}
