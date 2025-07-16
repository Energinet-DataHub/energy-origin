using System;
using System.Collections.Generic;
using System.Linq;
using API.Transfer.Api.Services;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;

namespace API.UnitTests.ReportGenerator.Utilities;

public class MockedDataGenerators
{
    public static List<ConsumptionHour> GenerateMockConsumption(int seed)
    {
        var rnd = new Random(seed);

        return Enumerable.Range(0, 24)
            .Select(h =>
            {
                var baseLoad = 20 + rnd.NextDouble() * 10;
                var hourlyFactor = h switch
                {
                    >= 0 and <= 5 => 0.5 + rnd.NextDouble() * 0.3,
                    >= 6 and <= 9 => 1.5 + rnd.NextDouble() * 0.5,
                    >= 10 and <= 16 => 1.0 + rnd.NextDouble() * 0.3,
                    >= 17 and <= 22 => 1.8 + rnd.NextDouble() * 0.4,
                    _ => 1.0 + rnd.NextDouble() * 0.2
                };

                var kWh = baseLoad * hourlyFactor * (0.9 + rnd.NextDouble() * 0.2);

                return new ConsumptionHour(h) { KwhQuantity = (decimal)Math.Round(kWh, 2) };
            })
            .ToList();
    }

    public static List<Claim> GenerateMockClaims(int seed, DateTimeOffset from, DateTimeOffset to, bool strictHourlyOnly = false)
    {
        var rnd = new Random(seed);

        var hrs = (int)(to - from).TotalHours;

        return Enumerable.Range(0, hrs)
            .Select(i =>
            {

                // For strict hourly claims, align to exact hours
                var timestamp = strictHourlyOnly
                    ? from.AddHours(i)
                    : from.AddHours(i).AddMinutes(rnd.Next(0, 60)); // Add random minutes

                var prodCert = new ClaimedCertificate
                {
                    FederatedStreamId = new FederatedStreamId { Registry = "dummy", StreamId = Guid.NewGuid() },
                    Start = timestamp.ToUnixTimeSeconds(),
                    End = timestamp.AddHours(1).ToUnixTimeSeconds(),
                    GridArea = string.Empty,
                    Attributes = new Dictionary<string, string>()
                };
                var conCert = new ClaimedCertificate
                {
                    FederatedStreamId = new FederatedStreamId { Registry = "dummy", StreamId = Guid.NewGuid() },
                    Start = timestamp.ToUnixTimeSeconds(),
                    End = timestamp.AddHours(1).ToUnixTimeSeconds(),
                    GridArea = string.Empty,
                    Attributes = new Dictionary<string, string>()
                };
                var hod = timestamp.Hour;
                var factor = hod switch
                {
                    >= 20 or <= 5 => 0,
                    <= 11 => (hod - 6) / 5.0 * 0.9,
                    <= 14 => 0.9 + (hod - 12) / 2.0 * 0.1,
                    _ => (19 - hod) / 4.0
                };

                const double maxProd = 120;
                var weather = 0.7 + rnd.NextDouble() * 0.3;
                var prod = maxProd * factor * weather * (0.9 + rnd.NextDouble() * 0.2) * 1000;

                return new Claim
                {
                    ClaimId = Guid.NewGuid(),
                    Quantity = (uint)Math.Round(prod, 0),
                    UpdatedAt = from.AddHours(i).ToUnixTimeSeconds(),
                    ProductionCertificate = prodCert,
                    ConsumptionCertificate = conCert
                };
            })
            .ToList();
    }
}
