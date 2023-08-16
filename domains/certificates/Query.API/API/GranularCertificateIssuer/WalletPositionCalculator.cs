using System;
using CertificateValueObjects;

namespace API.GranularCertificateIssuer;

public static class WalletPositionCalculator
{
    private static readonly long baseline = DateTimeOffset.Parse("2022-01-01T00:00:00Z").ToUnixTimeSeconds();

    public static int? Calculate(Period period)
    {
        var diff = period.DateFrom - baseline;

        if (diff < 0)
            return null;

        if (diff % 60 != 0)
            return null;

        var minutesElapsed = diff / 60;
        if (minutesElapsed > int.MaxValue)
            return null;

        return Convert.ToInt32(minutesElapsed);
    }
}
