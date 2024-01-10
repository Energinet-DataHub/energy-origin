using System.Text.Json.Serialization;
using API.MeteringPoints.Api.v2024_01_10.Dto.Responses.Enums;

namespace API.MeteringPoints.Api.v2024_01_10.Dto.Responses
{
    public class Measurement
    {
        [JsonPropertyName("gsrn")]
        public string? GSRN { get; set; }

        [JsonIgnore]
        public DateTimeOffset DateFrom { get; set; }

        [JsonIgnore]
        public DateTimeOffset DateTo { get; set; }

        [JsonPropertyName("quantity")]
        public long Quantity { get; set; }

        [JsonPropertyName("quality")]
        public EnergyQuantityValueQuality Quality { get; set; }

        [JsonPropertyName("dateFrom")]
        public long From => DateFrom.ToUnixTimeSeconds();

        [JsonPropertyName("dateTo")]
        public long To => DateTo.ToUnixTimeSeconds();
    }
}
