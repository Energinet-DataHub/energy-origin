using System.Text.Json.Serialization;

namespace AdminPortal.API.Models;

public class Api1Response
{
    [JsonPropertyName("result")]

    public required List<MeteringPoint> Result { get; set; }
}

public class Api2Response
{
    [JsonPropertyName("result")]
    public required List<Organization> Result { get; set; }
}

public class MeteringPoint
{
    public required string Id { get; set; }
    public required string Gsrn { get; set; }
    public required long Created { get; set; }
    public required string MeteringPointType { get; set; }
    public required string MeteringPointOwnerId { get; set; }
}

public class Organization
{
    public required string OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
    public required string Tin { get; set; }
}

public class AggregatedData
{
    public required string Gsrn { get; set; }
    public required string MeteringPointType { get; set; }
    public required string OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
    public required string Tin { get; set; }
}
