using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Services;
using API.Report;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using NSubstitute;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.Report;

public class EnergyChartGeneratorTests
{
    private static List<ConsumptionHour> GenerateMockConsumption(int seed)
    {
        var rnd = new Random(seed);

        return Enumerable.Range(0, 24)
            .Select(h =>
            {
                var baseLoad = 20 + rnd.NextDouble() * 10;

                var hourlyFactor = h switch
                {
                    >= 0 and <= 5 => 0.5 + (rnd.NextDouble() * 0.3),
                    >= 6 and <= 9 => 1.5 + (rnd.NextDouble() * 0.5),
                    >= 10 and <= 16 => 1.0 + (rnd.NextDouble() * 0.3),
                    >= 17 and <= 22 => 1.8 + (rnd.NextDouble() * 0.4),
                    _ => 1.0 + (rnd.NextDouble() * 0.2)
                };

                var consumption = baseLoad * hourlyFactor * (0.9 + rnd.NextDouble() * 0.2);

                return new ConsumptionHour(h)
                {
                    KwhQuantity = (decimal)Math.Round(consumption, 2)
                };
            })
            .ToList();
    }

    private static List<Claim> GenerateMockClaims(int seed, DateTimeOffset from, DateTimeOffset to)
    {
        var rnd = new Random(seed);
        var dummyCert = new ClaimedCertificate
        {
            FederatedStreamId = new FederatedStreamId { Registry = "dummy", StreamId = Guid.NewGuid() },
            Start = 0,
            End = 0,
            GridArea = string.Empty,
            Attributes = new Dictionary<string, string>()
        };

        var totalHours = (int)(to - from).TotalHours;

        return Enumerable.Range(0, totalHours)
            .Select(i =>
            {
                var hourOfDay = (from.Hour + i) % 24;

                var productionFactor = hourOfDay switch
                {
                    >= 20 or <= 5 => 0,
                    <= 11 => (hourOfDay - 6) / 5.0 * 0.9,
                    <= 14 => 0.9 + (hourOfDay - 12) / 2.0 * 0.1,
                    _ => (19 - hourOfDay) / 4.0
                };

                double maxProduction = 120;

                var weatherFactor = 0.7 + rnd.NextDouble() * 0.3;

                var production = maxProduction * productionFactor * weatherFactor * (0.9 + rnd.NextDouble() * 0.2);

                return new Claim
                {
                    ClaimId = Guid.NewGuid(),
                    Quantity = (uint)Math.Round(production, 0),
                    UpdatedAt = from.AddHours(i).ToUnixTimeSeconds(),
                    ProductionCertificate = dummyCert,
                    ConsumptionCertificate = dummyCert
                };
            })
            .ToList();
    }

    [Fact]
    public async Task GenerateAsync_SvgSnapshot()
    {
        var ownerGuid = Guid.NewGuid();
        var organizationId = OrganizationId.Create(ownerGuid);
        var from = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddYears(1);
        const int seed = 5;

        var mockCons = Substitute.For<IConsumptionService>();
        var consList = GenerateMockConsumption(seed);
        mockCons
            .GetTotalHourlyConsumption(organizationId, from, to, Arg.Any<CancellationToken>())
            .Returns(consList);

        var mockWallet = Substitute.For<IWalletClient>();
        var claimsList = GenerateMockClaims(seed, from, to);
        var claimsResult = new ResultList<Claim>
        {
            Result = claimsList,
            Metadata = new PageInfo { Count = claimsList.Count, Offset = 0, Limit = claimsList.Count, Total = claimsList.Count }
        };
        mockWallet
            .GetClaims(ownerGuid, from, to, Arg.Any<CancellationToken>())
            .Returns(claimsResult);

        var result = await EnergyChartGenerator.GenerateEnergySvgAsync(
            mockCons,
            mockWallet,
            organizationId,
            from,
            to);
        var svg = result.Svg;

        await Verifier.Verify(svg, extension: "svg");
    }
}
