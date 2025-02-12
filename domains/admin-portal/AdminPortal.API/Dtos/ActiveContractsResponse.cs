using System.Text.Json.Serialization;

namespace AdminPortal.API.Dtos;

public class ActiveContractsResponse
{
    public ResultsData Results { get; set; }
}

public class ResultsData
{
    public List<MeteringPoint> MeteringPoints { get; set; }
}

public class MeteringPoint
{
    public string GSRN { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MeteringPointType MeteringPointType { get; set; }

    public string OrganizationName { get; set; }
    public string Tin { get; set; }
    public long Created { get; set; }
    public long StartDate { get; set; }
    public long? EndDate { get; set; }
}
