using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace API.Models.Response
{
    public class MeasurementResponse
    {
        [JsonPropertyName("measurements")]
        public IEnumerable<AggregatedMeasurement> AggregatedMeasurement { get; }

        public MeasurementResponse(IEnumerable<AggregatedMeasurement> aggregatedMeasurement) => AggregatedMeasurement = aggregatedMeasurement;
    }
}
