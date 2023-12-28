using DataContext.ValueObjects;
using System.Globalization;
using System;

namespace RegistryConnector.Worker;

public static class WalletDepositEndpointPositionCalculator
{
    private static readonly long startDate = DateTimeOffset.Parse("2022-01-01T00:00:00Z", CultureInfo.InvariantCulture).ToUnixTimeSeconds();

    public static uint? CalculateWalletDepositEndpointPosition(this Period period)
    {
        var secondsElapsed = period.DateFrom - startDate;

        if (secondsElapsed < 0)
            return null;

        if (secondsElapsed % 60 != 0)
            return null;

        var minutesElapsed = secondsElapsed / 60;
        if (minutesElapsed > int.MaxValue)
            return null;

        return Convert.ToUInt32(minutesElapsed);
    }
}
