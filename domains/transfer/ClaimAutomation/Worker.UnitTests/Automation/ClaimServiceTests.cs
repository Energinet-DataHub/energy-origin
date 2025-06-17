using API.ClaimAutomation.Api.Repositories;
using ClaimAutomation.Worker;
using ClaimAutomation.Worker.Automation;
using ClaimAutomation.Worker.Metrics;
using ClaimAutomation.Worker.Options;
using DataContext.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Testing;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using Xunit;

namespace Worker.UnitTests.Automation;

public class ClaimServiceTests
{
    private readonly ILogger<ClaimService> logger = Substitute.For<ILogger<ClaimService>>();
    private readonly IClaimAutomationRepository claimRepository = Substitute.For<IClaimAutomationRepository>();
    private readonly IWalletClient walletClient = Substitute.For<IWalletClient>();
    private readonly IClaimAutomationMetrics metricsMock = Substitute.For<IClaimAutomationMetrics>();
    private readonly AutomationCache cacheMock = Substitute.For<AutomationCache>();

    private readonly IOptions<ClaimAutomationOptions> _claimAutomationOptions =
        new OptionsWrapper<ClaimAutomationOptions>(new ClaimAutomationOptions() { CertificateFetchBachSize = 2 });

    private readonly ClaimService claimService;

    public ClaimServiceTests()
    {
        claimService = new ClaimService(logger, claimRepository, walletClient, new Shuffler(1), metricsMock, cacheMock, _claimAutomationOptions);
    }

    [Fact]
    public async Task WhenClaimingFullProductionCertificate_CannotClaimProductionCertificateAgain()
    {
        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var start = DateTimeOffset.UtcNow;
        var end = DateTimeOffset.UtcNow.AddHours(1);
        uint quantity = 40;
        var consumptionCertificate1 = BuildCertificate(start, end, CertificateType.Consumption, quantity);
        var consumptionCertificate2 = BuildCertificate(start, end, CertificateType.Consumption, quantity);
        var productionCertificate = BuildCertificate(start, end, CertificateType.Production, quantity);
        using var cts = new CancellationTokenSource();

        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });

        ResultList<GranularCertificate> certs = new ResultList<GranularCertificate>()
        {
            Result = new List<GranularCertificate>
            {
                consumptionCertificate1,
                consumptionCertificate2,
                productionCertificate
            },
            Metadata = new PageInfo
            {
                Count = 3,
                Offset = 0,
                Total = 3,
                Limit = 100
            }
        };

        walletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>()).Returns(certs)
            .AndDoes(_ => cts.Cancel());


        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(1).ClaimCertificatesAsync(claimAutomationArgument.SubjectId, consumptionCertificate2, productionCertificate, quantity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenClaimingSomeOfTheProductionCertificate_CanClaimTheRestAfterwards()
    {
        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var start = DateTimeOffset.UtcNow;
        var end = DateTimeOffset.UtcNow.AddHours(1);
        uint consumptionQuantity = 40;
        uint productionQuantity = 70;
        var consumptionCertificate1 = BuildCertificate(start, end, CertificateType.Consumption, consumptionQuantity);
        var consumptionCertificate2 = BuildCertificate(start, end, CertificateType.Consumption, consumptionQuantity);
        var productionCertificate = BuildCertificate(start, end, CertificateType.Production, productionQuantity);
        using var cts = new CancellationTokenSource();

        ResultList<GranularCertificate> certs = new ResultList<GranularCertificate>()
        {
            Result = new List<GranularCertificate>
            {
                consumptionCertificate1,
                consumptionCertificate2,
                productionCertificate
            },
            Metadata = new PageInfo
            {
                Count = 3,
                Offset = 0,
                Total = 3,
                Limit = 100
            }
        };

        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });

        walletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>()).Returns(certs)
            .AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(1)
            .ClaimCertificatesAsync(claimAutomationArgument.SubjectId, consumptionCertificate2, productionCertificate, consumptionQuantity, Arg.Any<CancellationToken>());
        await walletClient.Received(1).ClaimCertificatesAsync(claimAutomationArgument.SubjectId, consumptionCertificate1, productionCertificate,
            productionQuantity - consumptionQuantity, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task WhenHittingBatchSize_ClaimAll(int numberOfCertificates)
    {
        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });

        var certs = BuildGranularCertificates(numberOfCertificates);
        using var cts = new CancellationTokenSource();

        var results = new List<ResultList<GranularCertificate>>();
        if (!certs.Any())
        {
            results.Add(new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = 0, Limit = _claimAutomationOptions.Value.CertificateFetchBachSize, Total = 0 },
                Result = []
            });
        }
        else
        {
            decimal nn2 = Decimal.Divide(numberOfCertificates, _claimAutomationOptions.Value.CertificateFetchBachSize);
            var nn3 = (int)Math.Ceiling(nn2);
            for (int i = 0; i < nn3; i++)
            {
                results.Add(new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo()
                    {
                        Offset = 0,
                        Count = certs.Skip(i * _claimAutomationOptions.Value.CertificateFetchBachSize)
                            .Take(_claimAutomationOptions.Value.CertificateFetchBachSize).Count(),
                        Limit = _claimAutomationOptions.Value.CertificateFetchBachSize,
                        Total = certs.Count
                    },
                    Result = certs.Skip(i * _claimAutomationOptions.Value.CertificateFetchBachSize)
                        .Take(_claimAutomationOptions.Value.CertificateFetchBachSize)
                });
            }
        }

        walletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(results[0], results.Skip(1).ToArray())
            .AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        var numberOfCertificatesUsedPrClaim = 2;
        await walletClient.Received(numberOfCertificates / numberOfCertificatesUsedPrClaim).ClaimCertificatesAsync(claimAutomationArgument.SubjectId,
            Arg.Any<GranularCertificate>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenTotalDecreasesOnNextCall_ClaimAll()
    {
        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });

        var certs = BuildGranularCertificates(4);

        using var cts = new CancellationTokenSource();

        walletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(new ResultList<GranularCertificate>()
            {
                Metadata =
                        new PageInfo() { Offset = 0, Count = 2, Limit = _claimAutomationOptions.Value.CertificateFetchBachSize, Total = certs.Count },
                Result = certs.Take(2)
            },
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = _claimAutomationOptions.Value.CertificateFetchBachSize, Total = 3 },
                    Result = certs.Skip(2).Take(1)
                })
            .AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(1).ClaimCertificatesAsync(claimAutomationArgument.SubjectId, Arg.Any<GranularCertificate>(),
            Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenTotalIncreasesOnNextCall_ClaimAll()
    {
        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });

        var certs = BuildGranularCertificates(5);

        using var cts = new CancellationTokenSource();

        walletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = _claimAutomationOptions.Value.CertificateFetchBachSize, Total = 4 },
                Result = certs.Take(2)
            },
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = _claimAutomationOptions.Value.CertificateFetchBachSize, Total = 5 },
                    Result = certs.Skip(2).Take(2)
                },
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = _claimAutomationOptions.Value.CertificateFetchBachSize, Total = 5 },
                    Result = certs.Skip(4).Take(1)
                })
            .AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(2).ClaimCertificatesAsync(claimAutomationArgument.SubjectId, Arg.Any<GranularCertificate>(),
            Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, null)]
    [InlineData(null, true)]
    public async Task Run_DoesNotCallClaimCertificatesAsync_WhenOnlyOneIsTrialIsTrue(bool? consumptionIsTrial, bool? productionIsTrial)
    {
        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });

        var productionCertificate = new GranularCertificate
        {
            FederatedStreamId = new FederatedStreamId
            {
                Registry = "test",
                StreamId = Guid.NewGuid()
            },
            Quantity = 10,
            Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            CertificateType = CertificateType.Production,
            Attributes = productionIsTrial != null ? new Dictionary<string, string>
            {
                { "IsTrial", productionIsTrial.ToString()! }
            } : new Dictionary<string, string>()
        };

        var consumptionCertificate = new GranularCertificate
        {
            FederatedStreamId = new FederatedStreamId
            {
                Registry = "test",
                StreamId = Guid.NewGuid()
            },
            Quantity = 10,
            Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            GridArea = "DK1",
            CertificateType = CertificateType.Consumption,
            Attributes = consumptionIsTrial != null ? new Dictionary<string, string>
            {
                { "IsTrial", consumptionIsTrial.ToString()! }
            } : new Dictionary<string, string>()
        };

        var certs = new ResultList<GranularCertificate>
        {
            Result = new List<GranularCertificate> { productionCertificate, consumptionCertificate },
            Metadata = new PageInfo { Count = 2, Offset = 0, Total = 2, Limit = 100 }
        };
        using var cts = new CancellationTokenSource();

        walletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(certs).AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.DidNotReceiveWithAnyArgs().ClaimCertificatesAsync(
            claimAutomationArgument.SubjectId,
            Arg.Any<GranularCertificate>(),
            Arg.Any<GranularCertificate>(),
            Arg.Any<uint>(),
            cts.Token);

    }

    private List<GranularCertificate> BuildGranularCertificates(int count)
    {
        var certs = new List<GranularCertificate>();
        for (var i = 0; i < count; i++)
        {
            certs.Add(BuildCertificate(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
                i % 2 == 0 ? CertificateType.Production : CertificateType.Consumption, 123));
        }

        return certs;
    }

    private static GranularCertificate BuildCertificate(DateTimeOffset start, DateTimeOffset end, CertificateType type, uint quantity)
    {
        var gridArea = "DK1";
        var registry = "SomeRegistry";
        var certificate = new GranularCertificate
        {
            Quantity = quantity,
            CertificateType = type,
            GridArea = gridArea,
            FederatedStreamId = new FederatedStreamId
            {
                Registry = registry,
                StreamId = Guid.NewGuid()
            },
            Start = start.ToUnixTimeSeconds(),
            End = end.ToUnixTimeSeconds(),
            Attributes = new Dictionary<string, string>()
            {
                { "assetId", Some.Gsrn() }
            }
        };

        if (type == CertificateType.Production)
        {
            certificate.Attributes.Add("techCode", Some.TechCode);
            certificate.Attributes.Add("fuelCode", Some.FuelCode);
        }

        return certificate;
    }
}
