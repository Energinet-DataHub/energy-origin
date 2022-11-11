using System.Threading;
using System.Threading.Tasks;
using API.GranularCertificateIssuer;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;
using FluentAssertions;
using Marten;
using Marten.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace API.UnitTests.GranularCertificateIssuer;

public class EnergyMeasuredConsumerTest
{
    private readonly MasterData validMasterData = new(
        GSRN: "gsrn",
        GridArea: "gridArea",
        Type: MeteringPointType.Production,
        Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointOnboarded: true);

    [Fact]
    public async Task Consume_NoMasterData_NoEventsSaved()
    {
        var documentSessionMock = new Mock<IDocumentSession>();

        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData(It.IsAny<string>()))
            .ReturnsAsync(value: null);

        var message = new Measurement(
            GSRN: "gsrn",
            Period: new Period(1, 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, masterDataServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ConsumptionPoint_NoEventsSaved()
    {
        var masterDataForConsumptionPoint = validMasterData with { Type = MeteringPointType.Consumption };
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData(It.IsAny<string>()))
            .ReturnsAsync(masterDataForConsumptionPoint);

        var documentSessionMock = new Mock<IDocumentSession>();

        var message = new Measurement(
            GSRN: masterDataForConsumptionPoint.GSRN,
            Period: new Period(1, 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, masterDataServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_MeteringPointNotOnboarded_NoEventsSaved()
    {
        var masterDataForNotOnboarded = validMasterData with { MeteringPointOnboarded = false };
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData(It.IsAny<string>()))
            .ReturnsAsync(masterDataForNotOnboarded);

        var documentSessionMock = new Mock<IDocumentSession>();

        var message = new Measurement(
            GSRN: masterDataForNotOnboarded.GSRN,
            Period: new Period(1, 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, masterDataServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ProductionPoint_EventsSaved()
    {
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData(It.IsAny<string>()))
            .ReturnsAsync(validMasterData);

        var documentSessionMock = new Mock<IDocumentSession>();
        documentSessionMock.Setup(m => m.Events)
            .Returns(Mock.Of<IEventStore>());

        var message = new Measurement(
            GSRN: validMasterData.GSRN,
            Period: new Period(1, 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, masterDataServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task PublishAndConsumeMessage(Measurement message, IDocumentSession documentSession, IMasterDataService masterDataService)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<EnergyMeasuredConsumer>())
            .AddSingleton(documentSession)
            .AddSingleton(masterDataService)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<Measurement>()).Should().BeTrue();
    }
}
