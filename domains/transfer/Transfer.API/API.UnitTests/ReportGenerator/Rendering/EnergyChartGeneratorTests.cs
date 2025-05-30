using System;
using System.Threading;
using System.Threading.Tasks;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;
using API.Transfer.Api.Services;
using API.UnitTests.ReportGenerator.Utilities;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using NSubstitute;
using VerifyXunit;
using Xunit;

namespace API.UnitTests.ReportGenerator.Rendering;
public class EnergySvgRendererTests
{
    [Theory]
    [InlineData(Language.Danish)]
    [InlineData(Language.English)]
    public async Task GivenHourlyData_WhenRenderingSvg_ThenSvgInChosenLanguageIsRendered(Language language)
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
        var strictClaims = MockedDataGenerators.GenerateMockClaims(seed, from, to, strictHourlyOnly: true);
        var allClaims = MockedDataGenerators.GenerateMockClaims(seed, from, to);

        wallet.GetClaims(orgId.Value, from, to, TimeMatch.Hourly, Arg.Any<CancellationToken>())
              .Returns(new ResultList<Claim>
              {
                  Result = strictClaims,
                  Metadata = new PageInfo
                  {
                      Count = allClaims.Count,
                      Offset = 0,
                      Limit = allClaims.Count,
                      Total = allClaims.Count
                  }
              });

        wallet.GetClaims(orgId.Value, from, to, TimeMatch.All, Arg.Any<CancellationToken>())
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
        var renderer = new EnergySvgRenderer();

        var (rawCons, strictProd, allProd) = await fetcher.GetAsync(orgId, from, to, TestContext.Current.CancellationToken);
        var hourly = EnergyDataProcessor.ToHourly(rawCons, strictProd, allProd);

        var svg = renderer.Render(hourly, language).Svg;

        await Verifier.Verify(svg, extension: "svg")
            .UseParameters(language)
            .UseDirectory("Snapshots");
    }
}
