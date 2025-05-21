using System;
using System.Threading;
using System.Threading.Tasks;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;
using API.Transfer.Api.Services;
using API.UnitTests.ReportGenerator.Utilities;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using NSubstitute;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator;

public class HeadlinePercentageRendererTests
{
    [Fact]
    public async Task Pipeline_GeneratesExpectedHeadlineBox_HtmlSnapshot()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());

        var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        const int seed = 7;

        var consSvc = Substitute.For<IConsumptionService>();
        var consHours = MockedDataGenerators.GenerateMockConsumption(seed);
        consSvc.GetTotalHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
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
        var headlineCalc = new HeadlinePercentageProcessor();
        var htmlRenderer = new HeadlinePercentageRenderer();

        var (rawCons, rawProd) = await fetcher.GetAsync(orgId, from, to, TestContext.Current.CancellationToken);
        var hourly = EnergyDataProcessor.ToHourly(rawCons, rawProd);
        var percent = headlineCalc.Render(hourly);
        var html = htmlRenderer.Render(percent, "Året 2024");

        await Verifier.Verify(html, extension: "html");
    }
}
