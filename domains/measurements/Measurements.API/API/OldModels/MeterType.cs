using System.Text.Json.Serialization;

namespace API.OldModels;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeterType
{
    Consumption,
    Production,
}
