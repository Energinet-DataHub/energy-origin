using System;
using System.Text.RegularExpressions;
using API.MeteringPoints.Api.Dto.Responses.Enums;
using API.MeteringPoints.Api.Models.Constants;

namespace API.MeteringPoints.Api.Dto.Responses;

public record MeteringPoint(string GSRN, string GridArea, MeterType Type, SubMeterType SubMeterType, Address Address, Technology Technology, bool CanBeUsedForIssuingCertificates, string Capacity)
{
    public static MeteringPoint CreateFrom(Meteringpoint.V1.MeteringPoint result)
    {
        Regex regex = new Regex("[ ]{2,}", RegexOptions.None);

        return new MeteringPoint(
            result.MeteringPointId,
            GetGridArea(result.Postcode),
            GetMeterType(result.TypeOfMp),
            GetSubMeterType(result.SubtypeOfMp),
            new Address(
                regex.Replace((result.StreetName + " " + result.BuildingNumber).Trim(), " "),
                regex.Replace((result.FloorId + " " + result.RoomId).Trim(), " "),
                null,
                result.CityName,
                result.Postcode,
                "DK"
            ),
            GetTechnology(result.AssetType),
            GetCanBeUsedForIssuingCertificates(result.TypeOfMp, result.AssetType),
            result.Capacity
        );
    }

    public static bool GetCanBeUsedForIssuingCertificates(string typeOfMp, string assetType)
    {
        if (GetMeterType(typeOfMp) == MeterType.Production)
        {
            if (GetAssetType(assetType) != AssetTypeEnum.Other)
            {
                return true;
            }
        }
        else if (GetMeterType(typeOfMp) == MeterType.Consumption)
        {
            return true;
        }

        return false;
    }

    public static string GetGridArea(string postcode)
    {
        var postcodeInt = int.Parse(postcode);

        if (postcodeInt >= 5000)
        {
            return "DK1";
        }
        else if (postcodeInt < 5000)
        {
            return "DK2";
        }

        throw new NotSupportedException($"Postcode '{postcode}' is out of bounds.");
    }

    public static SubMeterType GetSubMeterType(string subTypeOfMp)
    {
        if (subTypeOfMp == "D01")
        {
            return SubMeterType.Physical;
        }
        else if (subTypeOfMp == "D02")
        {
            return SubMeterType.Virtual;
        }
        else if (subTypeOfMp == "D03")
        {
            return SubMeterType.Calculated;
        }

        throw new NotSupportedException($"SubTypeOfMP '{subTypeOfMp}' is not supported.");
    }

    public static MeterType GetMeterType(string typeOfMp)
    {
        if (typeOfMp == "E17")
        {
            return MeterType.Consumption;
        }
        else if (typeOfMp == "E18")
        {
            return MeterType.Production;
        }
        else if (typeOfMp.StartsWith("D") && typeOfMp.Length == 3 && int.Parse(typeOfMp.Substring(1)) >= 1)
        {
            return MeterType.Child;
        }

        throw new NotSupportedException($"TypeOfMP '{typeOfMp}' is not supported.");
    }

    public static Technology GetTechnology(string assetType)
    {
        if (assetType == "D11")
        {
            return new Technology(AibTechCodeConstants.SolarPanel, AibFuelCodeConstants.Solar);
        }
        else if (assetType == "D12")
        {
            return new Technology(AibTechCodeConstants.WindTurbine, AibFuelCodeConstants.Wind);
        }

        return new Technology(AibTechCodeConstants.OtherTechnology, AibFuelCodeConstants.Other);
    }

    public static AssetTypeEnum GetAssetType(string assetType)
    {
        if (assetType == "D11")
        {
            return AssetTypeEnum.Solar;
        }
        else if (assetType == "D12")
        {
            return AssetTypeEnum.Wind;
        }

        return AssetTypeEnum.Other;
    }
}
