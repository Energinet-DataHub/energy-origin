using ClaimAutomation.Worker;
using ClaimAutomation.Worker.Api.Repositories;
using ClaimAutomation.Worker.Automation;
using ClaimAutomation.Worker.Automation.Services;
using ClaimAutomation.Worker.Metrics;
using DataContext.Models;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;
using Testing;
using Xunit;
using Attribute = ProjectOrigin.WalletSystem.V1.Attribute;

namespace Worker.UnitTests.Automation;

public class ClaimServiceTests
{

    [Fact]
    public async Task WhenClaimingFullProductionCertificate_CannotClaimProductionCertificateAgain()
    {
        var logger = Substitute.For<ILogger<ClaimService>>();
        var claimRepository = Substitute.For<IClaimAutomationRepository>();
        var poWalletService = Substitute.For<IProjectOriginWalletService>();
        var metricsMock = Substitute.For<IClaimAutomationMetrics>();
        var cacheMock = Substitute.For<AutomationCache>();

        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var start = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow);
        var end = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddHours(1));
        uint quantity = 40;
        var consumptionCertificate1 = BuildCertificate(start, end, GranularCertificateType.Consumption, quantity);
        var consumptionCertificate2 = BuildCertificate(start, end, GranularCertificateType.Consumption, quantity);
        var productionCertificate = BuildCertificate(start, end, GranularCertificateType.Production, quantity);
        using var cts = new CancellationTokenSource();

        claimRepository.GetClaimAutomationArguments().Returns(new List<ClaimAutomationArgument> { claimAutomationArgument });
        poWalletService.GetGranularCertificates(Arg.Any<Guid>()).Returns(new List<GranularCertificate>
        {
            consumptionCertificate1,
            consumptionCertificate2,
            productionCertificate
        }).AndDoes(_ => cts.Cancel());

        var claimService = new ClaimService(logger, claimRepository, poWalletService, new Shuffler(1), metricsMock, cacheMock);

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await poWalletService.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate2, productionCertificate, quantity);
    }

    [Fact]
    public async Task WhenClaimingSomeOfTheProductionCertificate_CanClaimTheRestAfterwards()
    {
        var logger = Substitute.For<ILogger<ClaimService>>();
        var claimRepository = Substitute.For<IClaimAutomationRepository>();
        var poWalletService = Substitute.For<IProjectOriginWalletService>();
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
        poWalletService.GetGranularCertificates(Arg.Any<Guid>()).Returns(new List<GranularCertificate>
        {
            consumptionCertificate1,
            consumptionCertificate2,
            productionCertificate
        }).AndDoes(_ => cts.Cancel());

        var claimService = new ClaimService(logger, claimRepository, poWalletService, new Shuffler(1), metricsMock, cacheMock);

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await poWalletService.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate2, productionCertificate, consumptionQuantity);
        await poWalletService.Received(1).ClaimCertificates(claimAutomationArgument.SubjectId, consumptionCertificate1, productionCertificate, productionQuantity - consumptionQuantity);
    }

    private static GranularCertificate BuildCertificate(Timestamp start, Timestamp end, GranularCertificateType type, uint quantity)
    {
        var gridArea = "DK1";
        var registry = "SomeRegistry";
        var certificate = new GranularCertificate
        {
            Quantity = quantity,
            Type = type,
            GridArea = gridArea,
            FederatedId = new FederatedStreamId
            {
                Registry = registry,
                StreamId = new Uuid
                {
                    Value = Guid.NewGuid().ToString()
                }
            },
            Start = start,
            End = end,
            Attributes = {
                new Attribute
                {
                    Key = "AssetId",
                    Value = Some.Gsrn()
                }
            }
        };

        if (type == GranularCertificateType.Production)
        {
            certificate.Attributes.Add(new Attribute { Key = "TechCode", Value = Some.TechCode });
            certificate.Attributes.Add(new Attribute { Key = "FuelCode", Value = Some.FuelCode });
        }

        return certificate;
    }
}
