using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using Meteringpoint.V1;

namespace API.UnitTests;

public class Any
{
    public static MeteringPointsResponse MeteringPointsResponse(Gsrn gsrn)
    {
        return new MeteringPointsResponse { MeteringPoints = { EnergyTrackAndTrace.Testing.Any.MeteringPoint(gsrn) } };
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
            Quality = EnergyQuality.Measured
        };
    }

    public static CertificateIssuingContract CertificateIssuingContract(Gsrn gsrn, UnixTimestamp start, UnixTimestamp? end, int contractNumber = 0)
    {
        return new CertificateIssuingContract()
        {
            GSRN = gsrn.Value,
            StartDate = start.ToDateTimeOffset(),
            EndDate = end?.ToDateTimeOffset(),
            ContractNumber = contractNumber
        };
    }

    public static MeteringPointTimeSeriesSlidingWindow MeteringPointTimeSeriesSlidingWindow(
        Gsrn? gsrn = null,
        UnixTimestamp? syncPoint = null,
        List<MeasurementInterval>? intervals = null)
    {
        return DataContext.Models.MeteringPointTimeSeriesSlidingWindow.Create(
            gsrn ?? Gsrn(),
            syncPoint ?? UnixTimestamp.Create(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            intervals ?? new List<MeasurementInterval>()
        );
    }
}
