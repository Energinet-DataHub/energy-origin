using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using NSubstitute;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator;

public class EnergySvgRendererTests
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

        var hrs = (int)(to - from).TotalHours;

        return Enumerable.Range(0, hrs)
            .Select(i =>
            {
                var hod = (from.Hour + i) % 24;
                var factor = hod switch
                {
                    >= 20 or <= 5 => 0,
                    <= 11 => (hod - 6) / 5.0 * 0.9,
                    <= 14 => 0.9 + (hod - 12) / 2.0 * 0.1,
                    _ => (19 - hod) / 4.0
                };

                const double maxProd = 120;
                var weather = 0.7 + rnd.NextDouble() * 0.3;
                var prod = maxProd * factor * weather * (0.9 + rnd.NextDouble() * 0.2);

                return new Claim
                {
                    ClaimId = Guid.NewGuid(),
                    Quantity = (uint)Math.Round(prod, 0),
                    UpdatedAt = from.AddHours(i).ToUnixTimeSeconds(),
                    ProductionCertificate = dummyCert,
                    ConsumptionCertificate = dummyCert
                };
            })
            .ToList();
    }

    [Fact]
    public async Task FullPipeline_GeneratesExpectedSvg_Snapshot()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddYears(1);
        const int seed = 5;

        var consSvc = Substitute.For<IConsumptionService>();
        var consHours = GenerateMockConsumption(seed);
        consSvc.GetTotalHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
               .Returns(consHours);

        var wallet = Substitute.For<IWalletClient>();
        var claims = GenerateMockClaims(seed, from, to);
        wallet.GetClaims(orgId.Value, from, to, Arg.Any<CancellationToken>())
              .Returns(new ResultList<Claim>
              {
                  Result = claims,
                  Metadata = new PageInfo
                  {
                      Count = claims.Count,
                      Offset = 0,
                      Limit = claims.Count,
                      Total = claims.Count
                  }
              });

        var fetcher = new EnergyDataFetcher(consSvc, wallet);
        var renderer = new EnergySvgRenderer();

        var (rawCons, rawProd) = await fetcher.GetAsync(orgId, from, to, TestContext.Current.CancellationToken);
        var hourly = EnergyDataProcessor.ToHourly(rawCons, rawProd);
        var svg = renderer.Render(hourly).Svg;

        await Verifier.Verify(svg, extension: "svg");
    }
}
