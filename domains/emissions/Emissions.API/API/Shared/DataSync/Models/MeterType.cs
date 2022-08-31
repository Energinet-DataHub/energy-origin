using System.Text.Json.Serialization;

namespace API.Shared.DataSync.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeterType
{
    Consumption,
    Production,
}
