using System.ComponentModel;
using System.Text.Json.Serialization;

namespace API.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Quality
{
    Measured,

    Revised,

    Calculated,

    Estimated,
}
