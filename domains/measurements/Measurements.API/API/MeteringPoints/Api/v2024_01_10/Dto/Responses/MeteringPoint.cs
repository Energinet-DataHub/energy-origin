using System;
using System.Text.RegularExpressions;
using API.MeteringPoints.Api.v2024_01_10.Dto.Responses.Constants;
using API.MeteringPoints.Api.v2024_01_10.Dto.Responses.Enums;

namespace API.MeteringPoints.Api.v2024_01_10.Dto.Responses;

public record MeteringPoint(string GSRN, string GridArea, MeterType Type, SubMeterType SubMeterType, Address Address, AssetTypeEnum AssetType, Technology Technology)
{
    public static MeteringPoint CreateFrom(Meteringpoint.V1.MeteringPoint result)
    {
        Regex regex = new Regex("[ ]{2,}", RegexOptions.None); //Multiple whitespaces

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
            GetAssetType(result.AssetType),
            GetTechnology(result.AssetType)
        );
    }

    private static string GetGridArea(string postcode)
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

    private static SubMeterType GetSubMeterType(string subTypeOfMp)
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
            return MeterType.consumption;
        }
        else if (typeOfMp == "E18")
        {
            return MeterType.production;
        }
        else if (typeOfMp.StartsWith("D") && typeOfMp.Length == 3 && int.Parse(typeOfMp.Substring(1)) >= 1)
        {
            return MeterType.child;
        }

        throw new NotSupportedException($"TypeOfMP '{typeOfMp}' is not supported.");
    }

    private static AssetTypeEnum GetAssetType(string assetType)
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

    private static Technology GetTechnology(string assetType)
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
}
