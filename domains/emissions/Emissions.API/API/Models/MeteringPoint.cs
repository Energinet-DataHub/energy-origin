using System.Text.Json.Serialization;

namespace API.Models;

public class MeteringPoint
{
    [JsonPropertyName("gsrn")]
    public long GSRN { get; set; }

    [JsonPropertyName("gridArea")]
    public string GridArea { get; set; }
}

public class MeteringPointsResponse
{
    public List<MeteringPoint> MeteringPoints { get; set; }
}