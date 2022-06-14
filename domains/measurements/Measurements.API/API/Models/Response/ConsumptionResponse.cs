 using System.Text.Json.Serialization;

namespace API.Models
{
    public class ConsumptionsResponse
    {
        [JsonPropertyName("consumptions")]
        public IEnumerable<Consumptions> Consumptions { get; }

        public ConsumptionsResponse(IEnumerable<Consumptions> consumptions)
        {
            Consumptions = consumptions;
        }
    }
}