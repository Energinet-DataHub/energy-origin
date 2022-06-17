using System.Text.Json.Serialization;

namespace API.Models
{
    public class EmissionsResponse
    {
        [JsonPropertyName("emissions")]
        public IEnumerable<Emissions> Emissions { get; }

        public EmissionsResponse(IEnumerable<Emissions> emissions)
        {
            Emissions = emissions;
        }
    }
}