using System;
using CertificateValueObjects;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker
{
    public static class PeriodExtension
    {
        public static DateInterval ToDateInterval(this Period period) =>
            new(
                DateTimeOffset.FromUnixTimeSeconds(period.DateFrom),
                DateTimeOffset.FromUnixTimeSeconds(period.DateTo)
            );
    }
}
