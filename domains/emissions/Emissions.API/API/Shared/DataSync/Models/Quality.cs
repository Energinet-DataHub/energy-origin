using System.Text.Json.Serialization;

namespace API.Shared.DataSync.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Quality
{
    Measured,

    Revised,

    Calculated,

    Estimated,
}
