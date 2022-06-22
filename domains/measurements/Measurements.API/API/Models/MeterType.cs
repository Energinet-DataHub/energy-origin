using System.Text.Json.Serialization;

namespace API.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeterType
{
    Consumption,
    Production,
}
