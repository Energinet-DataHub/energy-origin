using System;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// Metering point type of the contract. Can be Production or Consumption
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MeteringPointTypeResponse MeteringPointType { get; set; }

    public Technology? Technology { get; set; }

    public static Contract CreateFrom(DataContext.Models.CertificateIssuingContract contract) =>
        new()
        {
            Id = contract.Id,
            GSRN = contract.GSRN,
            StartDate = contract.StartDate.ToUnixTimeSeconds(),
            EndDate = contract.EndDate?.ToUnixTimeSeconds(),
            Created = contract.Created.ToUnixTimeSeconds(),
            MeteringPointType = contract.MeteringPointType.ToMeteringPointTypeResponse(),
            Technology = Technology.From(contract.Technology)
        };
}


public class Technology
{
    public string AibFuelCode { get; init; }
    public string AibTechCode { get; init; }

    public Technology(string AibFuelCode, string AibTechCode)
    {
        this.AibFuelCode = AibFuelCode;
        this.AibTechCode = AibTechCode;
    }

    public Technology()
    {

    }

    public static Technology? From(DataContext.ValueObjects.Technology? technology) => technology == null ? null : new(technology.FuelCode, technology.TechCode);
};

public static class MeteringPointTypeExtensions
{
    public static MeteringPointTypeResponse ToMeteringPointTypeResponse(this DataContext.ValueObjects.MeteringPointType meteringPointType)
    {
        return meteringPointType switch
        {
            DataContext.ValueObjects.MeteringPointType.Production => MeteringPointTypeResponse.Production,
            DataContext.ValueObjects.MeteringPointType.Consumption => MeteringPointTypeResponse.Consumption,
            _ => throw new ArgumentOutOfRangeException(nameof(meteringPointType), meteringPointType, null)
        };
    }
}


public enum MeteringPointTypeResponse
{
    Production,
    Consumption
}

