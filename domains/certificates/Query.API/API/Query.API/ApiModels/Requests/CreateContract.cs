using System.Text.Json.Serialization;
using CertificateValueObjects;

namespace API.Query.API.ApiModels.Requests;

public class CreateContract
{
    /// <summary>
    /// Global Service Relation Number (GSRN) for the metering point
    /// </summary>
    [JsonPropertyName("gsrn")]
    public string GSRN { get; init; } = "";

    /// <summary>
    /// Starting date for generation of certificates in Unix time seconds
    /// </summary>
    [JsonPropertyName("startDate")]
    public long StartDate { get; init; }

    /// <summary>
    /// End date for generation of certificates in Unix time seconds. Set to null for no end date
    /// </summary>
    [JsonPropertyName("endDate")]
    public long? EndDate { get; set; }

    /// <summary>
    /// Metering point type. Can be either Production or Consumption
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("meteringPointType")]
    public MeteringPointType? MeteringPointType { get; set; }
}
