using System.Text.Json.Serialization;

namespace API.Models
{
    public class MeasurementResponse
    {
        [JsonPropertyName("AggregatedMeasurement")]
        public IEnumerable<AggregatedMeasurement> AggregatedMeasurement { get; }

        public MeasurementResponse(IEnumerable<AggregatedMeasurement> aggregatedMeasurement)
        {
            AggregatedMeasurement = aggregatedMeasurement;
        }
    }
}
