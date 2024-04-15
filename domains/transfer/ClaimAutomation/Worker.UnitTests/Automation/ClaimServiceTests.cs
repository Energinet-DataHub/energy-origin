using ClaimAutomation.Worker;
using ClaimAutomation.Worker.Api.Repositories;
using ClaimAutomation.Worker.Automation;
using ClaimAutomation.Worker.Automation.Services;
using ClaimAutomation.Worker.Metrics;
using DataContext.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOriginClients.Models;
using Testing;
using Xunit;

namespace Worker.UnitTests.Automation;

public class ClaimServiceTests
{

    [Fact]
    public async Task WhenClaimingFullProductionCertificate_CannotClaimProductionCertificateAgain()
    {
        var logger = Substitute.For<ILogger<ClaimService>>();
        var claimRepository = Substitute.For<IClaimAutomationRepository>();
        var walletClient = Substitute.For<IWalletClient>();
        var metricsMock = Substitute.For<IClaimAutomationMetrics>();
        var cacheMock = Substitute.For<AutomationCache>();

        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var start = DateTimeOffset.UtcNow;
        var end = DateTimeOffset.UtcNow.AddHours(1);
        uint quantity = 40;
        var consumptionCertificate1 = BuildCertificate(start, end, CertificateType.Consumption, quantity);
        var consumptionCertificate2 = BuildCertificate(start, end, CertificateType.Consumption, quantity);
        var productionCertificate = BuildCertificate(start, end, CertificateType.Production, quantity);
        using var cts = new CancellationTokenSource();

        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });
        walletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<GranularCertificate>
        {
            consumptionCertificate1,
            consumptionCertificate2,
            productionCertificate
        }).AndDoes(_ => cts.Cancel());

        var claimService = new ClaimService(logger, claimRepository, walletClient, new Shuffler(1), metricsMock, cacheMock);

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate2, productionCertificate, quantity);
    }

    [Fact]
    public async Task WhenClaimingSomeOfTheProductionCertificate_CanClaimTheRestAfterwards()
    {
        var logger = Substitute.For<ILogger<ClaimService>>();
        var claimRepository = Substitute.For<IClaimAutomationRepository>();
        var walletClient = Substitute.For<IWalletClient>();
        var metricsMock = Substitute.For<IClaimAutomationMetrics>();
        var cacheMock = Substitute.For<AutomationCache>();

        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var start = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow);
        var end = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddHours(1));
        uint consumptionQuantity = 40;
        uint productionQuantity = 70;
        var consumptionCertificate1 = BuildCertificate(start, end, GranularCertificateType.Consumption, consumptionQuantity);
        var consumptionCertificate2 = BuildCertificate(start, end, GranularCertificateType.Consumption, consumptionQuantity);
        var productionCertificate = BuildCertificate(start, end, GranularCertificateType.Production, productionQuantity);
        using var cts = new CancellationTokenSource();

        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });
        walletClient.GetGranularCertificates(Arg.Any<Guid>()).Returns(new List<GranularCertificate>
        {
            consumptionCertificate1,
            consumptionCertificate2,
            productionCertificate
        }).AndDoes(_ => cts.Cancel());

        var claimService = new ClaimService(logger, claimRepository, walletClient, new Shuffler(1), metricsMock, cacheMock);

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await walletClient.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate2, productionCertificate, consumptionQuantity);
        await walletClient.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate1, productionCertificate, productionQuantity - consumptionQuantity);
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
            certificate.Attributes.Add( "techCode", Some.TechCode);
            certificate.Attributes.Add("fuelCode", Some.FuelCode);
        }

        return certificate;
    }
}
