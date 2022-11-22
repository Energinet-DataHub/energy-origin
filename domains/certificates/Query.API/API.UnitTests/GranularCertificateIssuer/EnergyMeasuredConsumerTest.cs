using System;
using System.Threading;
using System.Threading.Tasks;
using API.GranularCertificateIssuer;
using API.MasterDataService;
using CertificateEvents.Primitives;
using FluentAssertions;
using IntegrationEvents;
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
        MeteringPointOnboardedStartDate: DateTimeOffset.Now.AddDays(-1));

    private readonly DateTimeOffset now = DateTimeOffset.Now;

    [Fact]
    public async Task Consume_NoMasterData_NoEventsSaved()
    {
        var documentSessionMock = GetDocumentSessionMock();

        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData(It.IsAny<string>())).ReturnsAsync(value: null);

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: "gsrn",
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: 42,
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

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

        var documentSessionMock = GetDocumentSessionMock();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: masterDataForConsumptionPoint.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: 42,
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, masterDataServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task Consume_StartDateInTheFuture_NoEventsSaved(int days, int seconds)
    {
        var masterDataForNotOnboarded = validMasterData with
        {
            MeteringPointOnboardedStartDate = now.AddDays(days).AddSeconds(seconds)
        };
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData(It.IsAny<string>())).ReturnsAsync(masterDataForNotOnboarded);

        var documentSessionMock = GetDocumentSessionMock();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: masterDataForNotOnboarded.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: 42,
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, masterDataServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ProductionPoint_EventsSaved()
    {
        var masterDataServiceMock = new Mock<IMasterDataService>();
        masterDataServiceMock.Setup(m => m.GetMasterData(It.IsAny<string>())).ReturnsAsync(validMasterData);

        var documentSessionMock = GetDocumentSessionMock();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: validMasterData.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: 42,
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, masterDataServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IDocumentSession> GetDocumentSessionMock()
    {
        var documentSessionMock = new Mock<IDocumentSession>();
        documentSessionMock.Setup(m => m.Events).Returns(Mock.Of<IEventStore>());
        return documentSessionMock;
    }

    private static async Task PublishAndConsumeMessage(EnergyMeasuredIntegrationEvent message,
        IDocumentSession documentSession, IMasterDataService masterDataService)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<EnergyMeasuredConsumer>())
            .AddSingleton(documentSession)
            .AddSingleton(masterDataService)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<EnergyMeasuredIntegrationEvent>()).Should().BeTrue();
    }
}
