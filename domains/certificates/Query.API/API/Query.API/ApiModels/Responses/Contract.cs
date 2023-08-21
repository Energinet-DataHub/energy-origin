using System;
using System.Text.Json.Serialization;
using API.ContractService;

namespace API.Query.API.ApiModels.Responses;

public class Contract
{
    public Guid Id { get; set; }

    /// <summary>
    /// Global Service Relation Number (GSRN) for the metering point
    /// </summary>
    [JsonPropertyName("gsrn")]
    public string GSRN { get; set; } = "";

    /// <summary>
    /// Starting date for generation of certificates in Unix time seconds
    /// </summary>
    public long StartDate { get; set; }

    /// <summary>
    /// End date for generation of certificates in Unix time seconds. The value null indicates no end date
    /// </summary>
    public long? EndDate { get; set; }

    /// <summary>
    /// Creation date for the contract
    /// </summary>
    public long Created { get; set; }

    public static Contract CreateFrom(CertificateIssuingContract contract) =>
        new()
        {
            Id = contract.Id,
            GSRN = contract.GSRN,
            StartDate = contract.StartDate.ToUnixTimeSeconds(),
            EndDate = contract.EndDate?.ToUnixTimeSeconds(),
            Created = contract.Created.ToUnixTimeSeconds()
        };
}
