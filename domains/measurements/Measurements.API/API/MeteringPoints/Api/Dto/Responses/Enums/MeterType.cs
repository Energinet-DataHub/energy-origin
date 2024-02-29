using System.Text.Json.Serialization;

namespace API.MeteringPoints.Api.Dto.Responses.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MeterType
    {
        Consumption,
        Production,
        Child,
    }
}
