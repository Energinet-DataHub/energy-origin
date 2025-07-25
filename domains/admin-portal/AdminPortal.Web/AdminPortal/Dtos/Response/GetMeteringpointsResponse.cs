using AdminPortal.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AdminPortal.Dtos.Response;

public record GetMeteringpointsResponse(List<GetMeteringPointsResponseItem> Result);

public record GetMeteringPointsResponseItem(string GSRN,
    MeteringPointType Type,
    string GridArea,
    SubMeterType SubMeterType,
    Address Address,
    Technology Technology,
    string ConsumerCvr,
    bool CanBeUsedForIssuingCertificates,
    string Capacity,
    string BiddingZone);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubMeterType
{
    Physical,
    Virtual,
    Calculated
}

public record Address(
    string Address1,
    string? Address2,
    string? Locality,
    string City,
    string PostalCode,
    string Country,
    string MunicipalityCode,
    string CitySubDivisionName
)
{
    public override string ToString()
    {
        var parts = new List<string>
        {
            Address1,
            Address2,
            Locality,
            $"{PostalCode} {City}".Trim(),
            CitySubDivisionName,
            Country
        };

        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}

public record Technology(string AibTechCode, string AibFuelCode)
{
    public override string ToString()
    {
        if (AibTechCode == "T010000" && AibFuelCode == "F01040100")
            return "Solar";
        else if (AibTechCode == "T020000" && AibFuelCode == "F01050100")
            return "Wind";
        else
            return "Other";
    }
}
