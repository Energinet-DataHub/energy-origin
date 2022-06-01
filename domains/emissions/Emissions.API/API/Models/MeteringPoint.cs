using System.Text.Json.Serialization;

namespace API.Models;

public class MeteringPoint
{
    [JsonPropertyName("gsrn")]
    public long GSRN { get; }

    [JsonPropertyName("gridArea")]
    public string GridArea { get; }

    public MeteringPoint(long gsrn, string gridArea)
    {
        GSRN = gsrn;
        GridArea = gridArea;
    }
}

public class MeteringPointsResponse
{
    public List<MeteringPoint> MeteringPoints { get; }

    public MeteringPointsResponse(List<MeteringPoint> meteringPoints)
    {
        MeteringPoints = meteringPoints;
    }
}