using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.Query.API.ApiModels.Requests;

public class CreateContract
{
    /// <summary>
    /// Global Service Relation Number (GSRN) for the metering point
    /// </summary>
    [JsonPropertyName("gsrn")]
    [Required]
    public string GSRN { get; init; } = "";

    /// <summary>
    /// Starting date for generation of certificates in Unix time seconds
    /// </summary>
    [Required]
    public long StartDate { get; init; }

    /// <summary>
    /// End date for generation of certificates in Unix time seconds. Set to null for no end date
    /// </summary>
    public long? EndDate { get; set; }
}

public record CreateContracts(List<CreateContract> Contracts);
