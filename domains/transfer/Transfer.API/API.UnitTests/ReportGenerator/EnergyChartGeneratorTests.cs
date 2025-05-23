using System;
using System.Threading;
using System.Threading.Tasks;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using API.UnitTests.ReportGenerator.Utilities;
using NSubstitute;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator;

public class EnergySvgRendererTests
{
    [Fact]
    public async Task FullPipeline_GeneratesExpectedSvg_Snapshot()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddYears(1);
        const int seed = 5;

        var consSvc = Substitute.For<IConsumptionService>();
        var consHours = MockedDataGenerators.GenerateMockConsumption(seed);
        consSvc.GetAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
                   .Returns(consHours);

        var wallet = Substitute.For<IWalletClient>();
        var claims = MockedDataGenerators.GenerateMockClaims(seed, from, to);
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
