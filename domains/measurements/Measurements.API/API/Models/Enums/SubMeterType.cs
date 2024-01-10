using System.Text.Json.Serialization;

namespace API.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubMeterType
{
    Physical,
    Virtual,
    Calculated
}
