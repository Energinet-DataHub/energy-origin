 using System.Text.Json.Serialization;

namespace API.Models
{
    public class ConsumptionResponse
    {
        [JsonPropertyName("consumption")]
        public IEnumerable<Consumption> Consumption { get; }

        public ConsumptionResponse(IEnumerable<Consumption> consumption)
        {
            Consumption = consumption;
        }
    }
}
