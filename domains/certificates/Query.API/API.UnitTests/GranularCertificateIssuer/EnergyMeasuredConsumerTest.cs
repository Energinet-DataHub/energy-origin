using System;
using System.Threading;
using System.Threading.Tasks;
using AggregateRepositories;
using API.ContractService;
using API.GranularCertificateIssuer;
using CertificateEvents.Aggregates;
using DomainCertificate.Primitives;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MeasurementEvents;
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
    public async Task Consume_NoContract_NoEventsSaved()
    {
        var repositoryMock = new Mock<IProductionCertificateRepository>();

        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(value: null);

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock.Object, contractServiceMock.Object);

        repositoryMock.Verify(s => s.Save(It.IsAny<ProductionCertificate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ConsumptionPoint_NoEventsSaved()
    {
        mockContract.MeteringPointType = MeteringPointType.Consumption;
        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContract);

        var repositoryMock = new Mock<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock.Object, contractServiceMock.Object);

        repositoryMock.Verify(s => s.Save(It.IsAny<ProductionCertificate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(MeasurementQuality.Calculated)]
    [InlineData(MeasurementQuality.Revised)]
    [InlineData(MeasurementQuality.Estimated)]
    public async Task Consume_MeasurementQualityNotMeasured_NoEventsSaved(MeasurementQuality measurementQuality)
    {
        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContract);

        var repositoryMock = new Mock<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: measurementQuality);

        await PublishAndConsumeMessage(message, repositoryMock.Object, contractServiceMock.Object);


        repositoryMock.Verify(s => s.Save(It.IsAny<ProductionCertificate>(), It.IsAny<CancellationToken>()), Times.Never);
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

        var repositoryMock = new Mock<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock.Object, contractServiceMock.Object);

        repositoryMock.Verify(s => s.Save(It.IsAny<ProductionCertificate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public async Task Consume_QuantityIsZeroOrNegative_NoEventsSaved(long quantity)
    {
        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContract);

        var repositoryMock = new Mock<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: quantity,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock.Object, contractServiceMock.Object);

        repositoryMock.Verify(s => s.Save(It.IsAny<ProductionCertificate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ValidMeasurement_EventsSaved()
    {
        var contractServiceMock = new Mock<IContractService>();
        contractServiceMock.Setup(c => c.GetByGSRN(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContract);

        var repositoryMock = new Mock<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock.Object, contractServiceMock.Object);

        repositoryMock.Verify(s => s.Save(It.IsAny<ProductionCertificate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task PublishAndConsumeMessage(EnergyMeasuredIntegrationEvent message,
        IProductionCertificateRepository repository, IContractService contractService)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<EnergyMeasuredConsumer>())
            .AddSingleton(repository)
            .AddSingleton(contractService)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<EnergyMeasuredIntegrationEvent>()).Should().BeTrue();
    }
}
