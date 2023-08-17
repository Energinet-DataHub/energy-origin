using System;
using CertificateValueObjects;
using Google.Protobuf.WellKnownTypes;
using ProjectOrigin.Electricity.V1;

namespace RegistryConnector.Worker;

public static class PeriodExtension
{
    public static DateInterval ToDateInterval(this Period period) =>
        new()
        {
            Start = Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeSeconds(period.DateFrom)),
            End = Timestamp.FromDateTimeOffset(DateTimeOffset.FromUnixTimeSeconds(period.DateTo))
        };
}
