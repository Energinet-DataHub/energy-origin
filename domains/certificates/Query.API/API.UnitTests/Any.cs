using System;
using System.Linq;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using Measurements.V1;

namespace API.UnitTests;
using Meteringpoint.V1;

public class Any
{
    public static MeteringPointsResponse MeteringPointsResponse(Gsrn gsrn)
    {
        return new MeteringPointsResponse { MeteringPoints = { MeteringPoint(gsrn) } };
    }

    public static MeteringPoint MeteringPoint(Gsrn gsrn)
    {
        return new MeteringPoint
        {
            MeteringPointId = gsrn.Value,
            MeteringPointAlias = "alias",
            ConsumerStartDate = "consumerStartDate",
            Capacity = "123",
            BuildingNumber = "buildingNumber",
            CityName = "cityName",
            Postcode = "postcode",
            StreetName = "streetName",
        };
    }

    public static Gsrn Gsrn()
    {
        return new Gsrn("57" + IntString(16));
    }

    private static string IntString(int charCount)
    {
        var alphabet = "0123456789";
        var random = new Random();
        var characterSelector = new Func<int, string>(_ => alphabet.Substring(random.Next(0, alphabet.Length), 1));
        return Enumerable.Range(1, charCount).Select(characterSelector).Aggregate((a, b) => a + b);
    }

    public static Technology Technology()
    {
        return new Technology("T12345", "T54321");
    }

    public static Measurement Measurement(Gsrn gsrn, long dateFrom, long value)
    {
        return new Measurement
        {
            Gsrn = gsrn.Value,
            DateFrom = dateFrom,
            DateTo = dateFrom + 3600,
            Quantity = value,
            Quality = EnergyQuantityValueQuality.Measured
        };
    }

    public static CertificateIssuingContract CertificateIssuingContract(Gsrn gsrn, UnixTimestamp start, UnixTimestamp? end)
    {
        return new CertificateIssuingContract()
        {
            GSRN = gsrn.Value,
            StartDate = start.ToDateTimeOffset(),
            EndDate = end?.ToDateTimeOffset()
        };
    }
}
