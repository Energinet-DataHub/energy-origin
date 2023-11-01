using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Claiming.Api.Models;
using API.Claiming.Api.Repositories;
using API.Claiming.Automation;
using API.Claiming.Automation.Services;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;
using Xunit;
using Attribute = ProjectOrigin.WalletSystem.V1.Attribute;

namespace API.UnitTests.Claiming.Automation;

public class ClaimServiceTests
{

    [Fact]
    public async Task WhenClaimingFullProductionCertificate_CannotClaimProductionCertificateAgain()
    {
        var logger = Substitute.For<ILogger<ClaimService>>();
        var claimRepository = Substitute.For<IClaimAutomationRepository>();
        var poWalletService = Substitute.For<IProjectOriginWalletService>();

        var claimSubject = new ClaimSubject(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var start = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow);
        var end = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddHours(1));
        uint quantity = 40;
        var consumptionCertificate1 = BuildCertificate(start, end, GranularCertificateType.Consumption, quantity);
        var consumptionCertificate2 = BuildCertificate(start, end, GranularCertificateType.Consumption, quantity);
        var productionCertificate = BuildCertificate(start, end, GranularCertificateType.Production, quantity);
        using var cts = new CancellationTokenSource();

        claimRepository.GetClaimSubjects().Returns(new List<ClaimSubject> { claimSubject });
        poWalletService.GetGranularCertificates(Arg.Any<Guid>()).Returns(new List<GranularCertificate>
        {
            consumptionCertificate1,
            consumptionCertificate2,
            productionCertificate
        }).AndDoes(_ => cts.Cancel());

        var claimService = new ClaimService(logger, claimRepository, poWalletService);

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await poWalletService.Received(1).ClaimCertificates(claimSubject.SubjectId, consumptionCertificate1, productionCertificate, quantity);
    }

    [Fact]
    public async Task WhenClaimingSomeOfTheProductionCertificate_CanClaimTheRestAfterwards()
    {
        var logger = Substitute.For<ILogger<ClaimService>>();
        var claimRepository = Substitute.For<IClaimAutomationRepository>();
        var poWalletService = Substitute.For<IProjectOriginWalletService>();

        var claimSubject = new ClaimSubject(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var start = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow);
        var end = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddHours(1));
        uint consumptionQuantity = 40;
        uint productionQuantity = 70;
        var consumptionCertificate1 = BuildCertificate(start, end, GranularCertificateType.Consumption, consumptionQuantity);
        var consumptionCertificate2 = BuildCertificate(start, end, GranularCertificateType.Consumption, consumptionQuantity);
        var productionCertificate = BuildCertificate(start, end, GranularCertificateType.Production, productionQuantity);
        using var cts = new CancellationTokenSource();

        claimRepository.GetClaimSubjects().Returns(new List<ClaimSubject> { claimSubject });
        poWalletService.GetGranularCertificates(Arg.Any<Guid>()).Returns(new List<GranularCertificate>
        {
            consumptionCertificate1,
            consumptionCertificate2,
            productionCertificate
        }).AndDoes(_ => cts.Cancel());

        var claimService = new ClaimService(logger, claimRepository, poWalletService);

        var act = async () => await claimService.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        await poWalletService.Received(1).ClaimCertificates(claimSubject.SubjectId, consumptionCertificate1, productionCertificate, consumptionQuantity);
        await poWalletService.Received(1).ClaimCertificates(claimSubject.SubjectId, consumptionCertificate2, productionCertificate, productionQuantity - consumptionQuantity);
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
