using ClaimAutomation.Worker;
using ClaimAutomation.Worker.Api.Repositories;
using ClaimAutomation.Worker.Automation;
using ClaimAutomation.Worker.Metrics;
using DataContext.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOriginClients;
using ProjectOriginClients.Models;
using Testing;
using Xunit;

namespace Worker.UnitTests.Automation;

public class ClaimServiceTsts
{
    private readonly ILogger<ClaimService> logger = Substitute.For<ILogger<ClaimService>>();
    private readonly IClaimAutomationRepository claimRepository = Substitute.For<IClaimAutomationRepository>();
    private readonly IProjectOriginWalletClient walletClient = Substitute.For<IProjectOriginWalletClient>();
    private readonly IClaimAutomationMetrics metricsMock = Substitute.For<IClaimAutomationMetrics>();
    private readonly AutomationCache cacheMock = Substitute.For<AutomationCache>();

    private readonly int batchSize = 2;

    private readonly ClaimService claimService;
    public ClaimServiceTsts()
    {
        claimService = new ClaimService(logger, claimRepository, walletClient, new Shuffler(1), metricsMock, cacheMock)
            { BatchSize = batchSize };
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

        walletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>()).Returns(certs).AndDoes(_ => cts.Cancel());


        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate2, productionCertificate, quantity);
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

        walletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>()).Returns(certs).AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate2, productionCertificate, consumptionQuantity);
        await walletClient.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate1, productionCertificate, productionQuantity - consumptionQuantity);
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
                Metadata = new PageInfo() { Offset = 0, Count = 0, Limit = batchSize, Total = 0 },
                Result = []
            });
        }
        else
        {
            decimal nn2 = Decimal.Divide(numberOfCertificates, batchSize);
            var nn3 = (int)Math.Ceiling(nn2);
            for (int i = 0; i < nn3; i++)
            {
                results.Add(new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = certs.Skip(i * batchSize).Take(batchSize).Count(), Limit = batchSize, Total = certs.Count },
                    Result = certs.Skip(i * batchSize).Take(batchSize)
                });
            }
        }

        walletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(results[0], results.Skip(1).ToArray())
            .AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(numberOfCertificates / 2).ClaimCertificates(claimAutomationArgument.SubjectId, Arg.Any<GranularCertificate>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>());
    }

    [Fact]
    public async Task WhenTotalDecreasesOnNextCall_ClamAll()
    {
        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });

        var certs = BuildGranularCertificates(4);

        using var cts = new CancellationTokenSource();

        walletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = certs.Count },
                Result = certs.Take(2)
            },
            new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = 3 },
                Result = certs.Skip(2).Take(1)
            })
            .AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, Arg.Any<GranularCertificate>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>());
    }

    [Fact]
    public async Task WhenTotalIncreasesOnNextCall_ClamAll()
    {
        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });

        var certs = BuildGranularCertificates(5);

        using var cts = new CancellationTokenSource();

        walletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = 4},
                    Result = certs.Take(2)
                },
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = 5 },
                    Result = certs.Skip(2).Take(2)
                },
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = 5 },
                    Result = certs.Skip(4).Take(1)
                })
            .AndDoes(_ => cts.Cancel());

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(2).ClaimCertificates(claimAutomationArgument.SubjectId, Arg.Any<GranularCertificate>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>());
    }

    private List<GranularCertificate> BuildGranularCertificates(int count)
    {
        var certs = new List<GranularCertificate>();
        for (var i = 0; i < count; i++)
        {
            certs.Add(BuildCertificate(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), i % 2 == 0 ? CertificateType.Production : CertificateType.Consumption, 123 + (uint)i));
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
            Attributes = new Dictionary<string, string>() {
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
