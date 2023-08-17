using System;
using System.Globalization;
using CertificateValueObjects;

namespace API.GranularCertificateIssuer;

public static class WalletPositionCalculator
{
    private static readonly long startDate = DateTimeOffset.Parse("2022-01-01T00:00:00Z", CultureInfo.InvariantCulture).ToUnixTimeSeconds();

    public static int? CalculateWalletPosition(this Period period)
    {
        var secondsElapsed = period.DateFrom - startDate;

        if (secondsElapsed < 0)
            return null;

        if (secondsElapsed % 60 != 0)
            return null;

        var minutesElapsed = secondsElapsed / 60;
        if (minutesElapsed > int.MaxValue)
            return null;

        return Convert.ToInt32(minutesElapsed);
    }
}
