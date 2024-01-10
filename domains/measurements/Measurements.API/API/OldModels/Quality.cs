using System.Text.Json.Serialization;

namespace API.OldModels;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Quality
{
    Measured,

    Revised,

    Calculated,

    Estimated,
}
