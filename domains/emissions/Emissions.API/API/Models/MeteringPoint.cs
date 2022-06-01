using System.Text.Json.Serialization;

namespace API.Models;

public class MeteringPoint
{
    [JsonPropertyName("gsrn")]
    public long Gsrn { get; set; }
    public string GridArea { get; set; }
    public string WebAccessCode { get; set; }
}

public class MeteringPointsResponse
{
    public List<MeteringPoint> MeteringPoints { get; set; }
}