using System.Text.Json.Serialization;

namespace API.Shared.DataSync.Models;

public record MeteringPoint
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

public record MeteringPointsResponse
{
    public List<MeteringPoint> MeteringPoints { get; }

    public MeteringPointsResponse(List<MeteringPoint> meteringPoints) => MeteringPoints = meteringPoints;
}
