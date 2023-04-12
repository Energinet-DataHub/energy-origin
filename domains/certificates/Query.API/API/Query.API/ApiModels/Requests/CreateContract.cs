using System.Text.Json.Serialization;

namespace API.Query.API.ApiModels.Requests;

public class CreateContract
{
    /// <summary>
    /// Global Service Relation Number (GSRN) for the metering point
    /// </summary>
    [JsonPropertyName("gsrn")]
    public string GSRN { get; init; } = "";

    /// <summary>
    /// Starting date for generation of certificates in Unix time
    /// </summary>
    public long StartDate { get; init; }

    /// <summary>
    /// OPTIONAL: End date for generation of certificates in Unix time
    /// </summary>
    public long? EndDate { get; set; }
}
