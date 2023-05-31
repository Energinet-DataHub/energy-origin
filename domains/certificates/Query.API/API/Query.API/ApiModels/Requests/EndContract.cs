using System;
using Newtonsoft.Json;

namespace API.Query.API.ApiModels.Requests;

public class EndContract
{
    /// <summary>
    /// Global Service Relation Number (GSRN) for the metering point
    /// </summary>
    [JsonProperty("gsrn")]
    public Guid ContractId { get; set; }

    /// <summary>
    /// End Date for generation of certificates in Unix time
    /// </summary>
    public long? EndDate { get; set; }
}
