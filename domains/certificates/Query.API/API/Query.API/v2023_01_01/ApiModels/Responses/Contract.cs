using System;
using System.Text.Json.Serialization;
using DataContext.Models;
using DataContext.ValueObjects;

namespace API.Query.API.v2023_01_01.ApiModels.Responses;

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

    /// <summary>
    /// Metering point type of the contract. Can be Production or Consumption
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MeteringPointType MeteringPointType { get; set; }

    public Technology? Technology { get; set; }

    public static Contract CreateFrom(CertificateIssuingContract contract) =>
        new()
        {
            Id = contract.Id,
            GSRN = contract.GSRN,
            StartDate = contract.StartDate.ToUnixTimeSeconds(),
            EndDate = contract.EndDate?.ToUnixTimeSeconds(),
            Created = contract.Created.ToUnixTimeSeconds(),
            MeteringPointType = contract.MeteringPointType,
            Technology = contract.Technology
        };
}
