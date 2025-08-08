using System.Collections.Generic;

namespace API.ContractService.Models;

public record MeteringPointsResponse(List<MeteringPoint> Result);

public record MeteringPoint(
    string Gsrn,
    string GridArea,
    MeterType MeteringPointType,
    SubMeterType SubMeterType,
    Address Address,
    Technology Technology,
    string ConsumerCvr,
    bool CanBeUsedForIssuingCertificates,
    string Capacity,
    string BiddingZone);

public enum MeterType
{
    Consumption,
    Production,
    Child
}

public enum SubMeterType
{
    Physical,
    Virtual,
    Calculated
}

public record Technology(string AibFuelCode, string AibTechCode);

public record Address(
    string Address1,
    string? Address2,
    string? Locality,
    string City,
    string PostalCode,
    string Country);
