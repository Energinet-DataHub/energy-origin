using System.Text.Json.Serialization;

namespace API.Query.API.ApiModels;

public class Certificate
{
    /// <summary>
    /// Start timestamp for the certificate in Unix time
    /// </summary>
    public long DateFrom { get; set; }

    /// <summary>
    /// End timestamp for the certificate in Unix time
    /// </summary>
    public long DateTo { get; set; }

    /// <summary>
    /// Quantity of energy measured in Wh
    /// </summary>
    public long Quantity { get; set; }

    /// <summary>
    /// Global Service Relation Number (GSRN) for the metering point
    /// </summary>
    [JsonPropertyName("gsrn")]
    public string GSRN { get; set; } = "";
}
