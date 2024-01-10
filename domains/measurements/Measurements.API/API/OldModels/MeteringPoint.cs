using System.Text.Json.Serialization;
using API.Models;

namespace API.OldModels;

public class MeteringPoint
{
    [JsonPropertyName("gsrn")]
    public string GSRN { get; }
    [JsonPropertyName("gridArea")]
    public string GridArea { get; }
    [JsonPropertyName("type")]
    public MeterType Type { get; }
    public MeteringPoint(string gsrn, string gridArea, MeterType type)
    {
        GSRN = gsrn;
        GridArea = gridArea;
        Type = type;
    }
}
