using System;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.GranularCertificateIssuer;
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
    private static readonly DateTimeOffset now = DateTimeOffset.Now;

    private readonly CertificateIssuingContract mockContract = new()
    {
        Id = Guid.NewGuid(),
        GSRN = "gsrn",
        GridArea = "gridArea",
        MeteringPointType = MeteringPointType.Production,
        MeteringPointOwner = "owner",
        StartDate = now.UtcDateTime,
        Created = now.UtcDateTime.AddDays(-1)
    };

    [Fact]
    public async Task Consume_NoMasterData_NoEventsSaved()
    {
        var documentSessionMock = GetDocumentSessionMock();

        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(value: null);

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, contractServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ConsumptionPoint_NoEventsSaved()
    {
        mockContract.MeteringPointType = MeteringPointType.Consumption;
        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContract);

        var documentSessionMock = GetDocumentSessionMock();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, contractServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task Consume_StartDateInTheFuture_NoEventsSaved(int days, int seconds)
    {
        mockContract.StartDate = now.AddDays(days).AddSeconds(seconds);

        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContract);

        var documentSessionMock = GetDocumentSessionMock();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, contractServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ProductionPoint_EventsSaved()
    {
        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContract);

        var documentSessionMock = GetDocumentSessionMock();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, documentSessionMock.Object, contractServiceMock.Object);

        documentSessionMock.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IDocumentSession> GetDocumentSessionMock()
    {
        var documentSessionMock = new Mock<IDocumentSession>();
        documentSessionMock.Setup(m => m.Events).Returns(Mock.Of<IEventStore>());
        return documentSessionMock;
    }

    private static async Task PublishAndConsumeMessage(EnergyMeasuredIntegrationEvent message,
        IDocumentSession documentSession, IContractService contractService)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<EnergyMeasuredConsumer>())
            .AddSingleton(documentSession)
            .AddSingleton(contractService)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<EnergyMeasuredIntegrationEvent>()).Should().BeTrue();
    }
}
