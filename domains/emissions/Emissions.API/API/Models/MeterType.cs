using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace API.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeterType
{
    consumption,
    production,
}
