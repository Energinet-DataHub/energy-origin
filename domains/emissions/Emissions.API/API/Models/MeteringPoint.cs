using System.Text.Json.Serialization;

namespace API.Models;

public class MeteringPoint
{
    [JsonPropertyName("gsrn")]
    public long Gsrn { get; set; }
    
    [JsonPropertyName("biddingZone")]
    public string GridArea { get; set; }
}