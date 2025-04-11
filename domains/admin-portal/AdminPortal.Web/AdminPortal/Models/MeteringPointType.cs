using System.Text.Json.Serialization;

namespace AdminPortal.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeteringPointType
{
    Consumption,
    Production
}
