using System;
using System.Collections.Generic;
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
        var consAverageHours = MockedDataGenerators.GenerateMockConsumption(seed);
        consSvc.GetTotalAndAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
                   .Returns((new List<ConsumptionHour>(), consAverageHours));

        var wallet = Substitute.For<IWalletClient>();
        var strictClaims = MockedDataGenerators.GenerateMockClaims(seed, from, to, strictHourlyOnly: true);
        var allClaims = MockedDataGenerators.GenerateMockClaims(seed, from, to);

        // Mock wallet client to return different claims based on TimeMatch
        wallet.GetClaimsAsync(
                orgId.Value,
                from,
                to,
                Arg.Is<TimeMatch>(t => t == TimeMatch.Hourly), // Strict hourly
                Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = strictClaims,
                Metadata = new PageInfo
                {
                    Count = strictClaims.Count,
                    Offset = 0,
                    Limit = strictClaims.Count,
                    Total = strictClaims.Count
                }
            });

        wallet.GetClaimsAsync(
                orgId.Value,
                from,
                to,
                Arg.Is<TimeMatch>(t => t == TimeMatch.All), // All claims
                Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = allClaims,
                Metadata = new PageInfo
                {
                    Count = allClaims.Count,
                    Offset = 0,
                    Limit = allClaims.Count,
                    Total = allClaims.Count
                }
            });

        var fetcher = new EnergyDataFetcher(consSvc, wallet);
        var formatter = new EnergyDataFormatter();
        var renderer = new EnergySvgRenderer();

        // Fetch all three datasets
        var (_, rawAverageHourConsumption, claims) = await fetcher.GetAsync(orgId, from, to, false, TestContext.Current.CancellationToken);
        var (rawCons, strictProd, allProd) = formatter.Format(rawAverageHourConsumption, claims);

        // Pass all three to processor
        var hourly = EnergyDataProcessor.ToHourly(rawCons, strictProd, allProd);
        var svg = renderer.Render(hourly).Svg;

        await Verifier.Verify(svg, extension: "svg");
    }
}
