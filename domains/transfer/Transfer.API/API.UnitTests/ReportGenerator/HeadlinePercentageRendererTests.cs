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

namespace API.UnitTests.ReportGenerator;

public class HeadlinePercentageRendererTests
{
    [Theory]
    [InlineData(Language.Danish)]
    [InlineData(Language.English)]
    public async Task GivenLanguage_WhenRenderingHeadlineBox_ThenCorrectLocalizedHtmlIsGenerated(Language language)
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        const int seed = 7;

        var period = $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";

        var consumptionClient = Substitute.For<IConsumptionService>();
        var consumptionHours = MockedDataGenerators.GenerateMockConsumption(seed);
        consumptionClient
            .GetAverageHourlyConsumption(orgId, from, to, Arg.Any<CancellationToken>())
            .Returns(consumptionHours);

        var strictClaims = MockedDataGenerators.GenerateMockClaims(seed, from, to, strictHourlyOnly: true);
        var allClaims = MockedDataGenerators.GenerateMockClaims(seed, from, to, strictHourlyOnly: false);

        var walletClient = Substitute.For<IWalletClient>();
        walletClient
            .GetClaims(orgId.Value, from, to, TimeMatch.Hourly, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = strictClaims,
                Metadata = new PageInfo { Count = strictClaims.Count, Offset = 0, Limit = strictClaims.Count, Total = strictClaims.Count }
            });
        walletClient
            .GetClaims(orgId.Value, from, to, TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = allClaims,
                Metadata = new PageInfo { Count = allClaims.Count, Offset = 0, Limit = allClaims.Count, Total = allClaims.Count }
            });

        var fetcher = new EnergyDataFetcher(consumptionClient, walletClient);
        var headlineProcessor = new HeadlinePercentageProcessor();
        var headlineRenderer = new HeadlinePercentageRenderer();

        var (rawCons, rawProdStrict, rawProdAll) =
            await fetcher.GetAsync(orgId, from, to, TestContext.Current.CancellationToken);

        var hourly = EnergyDataProcessor.ToHourly(rawCons, rawProdStrict, rawProdAll);
        var percent = headlineProcessor.Calculate(hourly);

        var html = headlineRenderer.Render(percent, period, language);

        await Verifier.Verify(html, extension: "html").UseParameters(language);
    }
}
