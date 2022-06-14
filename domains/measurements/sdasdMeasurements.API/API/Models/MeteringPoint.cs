using System.Text.Json.Serialization;

namespace API.Models;

public class MeteringPoint
{
    [JsonPropertyName("gsrn")]
    public string GSRN { get; }

    public MeteringPoint(string gsrn)
    {
        GSRN = gsrn;
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
