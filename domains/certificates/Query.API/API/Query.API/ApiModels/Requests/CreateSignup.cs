using System.Text.Json.Serialization;

namespace API.Query.API.ApiModels.Requests;

public class CreateSignup
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
}
//TODO: Can GSRN be a long or other type? GSRN is a fixed 18 digits number. Not sure if 1st number can be 0
//TODO: How does datasyncsyncer handle start times not on an even hour?
