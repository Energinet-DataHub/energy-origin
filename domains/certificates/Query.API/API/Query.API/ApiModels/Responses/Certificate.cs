using System;
using System.Text.Json.Serialization;

namespace API.Query.API.ApiModels.Responses;

public enum CertificateType
{
    Consumption,
    Production
}

public class Certificate
{
    /// <summary>
    /// Certificate ID
    /// </summary>
    public Guid Id { get; set; }

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

    /// <summary>
    /// Grid area for the metering point
    /// </summary>
    public string GridArea { get; set; } = "";

    /// <summary>
    /// The technology of the production device as specified in EECS Rules Fact Sheet 5
    /// </summary>
    public string TechCode { get; set; } = "";

    /// <summary>
    /// The energy source for the production device as specified in EECS Rules Fact Sheet 5
    /// </summary>
    public string FuelCode { get; set; } = "";

    /// <summary>
    /// The type of the certificate. Can be either Production, Consumption or Invalid
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CertificateType CertificateType { get; set; }
}
