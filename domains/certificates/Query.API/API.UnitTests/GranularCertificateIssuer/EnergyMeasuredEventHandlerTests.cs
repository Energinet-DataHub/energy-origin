using System;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.Data;
using API.GranularCertificateIssuer;
using CertificateValueObjects;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MeasurementEvents;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace API.UnitTests.GranularCertificateIssuer;

public class EnergyMeasuredEventHandlerTests
{
    private static readonly DateTimeOffset n = DateTimeOffset.UtcNow;
    private static readonly DateTimeOffset now = new(n.Year, n.Month, n.Day, n.Hour, n.Minute, 0, n.Offset); // Rounded to nearest minute

    private readonly CertificateIssuingContract productionMockContract = new()
    {
        Id = Guid.NewGuid(),
        GSRN = "gsrn",
        GridArea = "gridArea",
        MeteringPointType = MeteringPointType.Production,
        MeteringPointOwner = "owner",
        StartDate = now,
        EndDate = null,
        Created = now.AddDays(-1)
    };

    private readonly CertificateIssuingContract consumptionMockContract = new()
    {
        Id = Guid.NewGuid(),
        GSRN = "gsrn",
        GridArea = "gridArea",
        MeteringPointType = MeteringPointType.Consumption,
        MeteringPointOwner = "owner",
        StartDate = now,
        EndDate = null,
        Created = now.AddDays(-1)
    };

    [Fact]
    public async Task Consume_NoContract_NoEventsSaved()
    {
        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsNullForAnyArgs();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_ConsumptionPoint_NoEventsSaved()
    {
        productionMockContract.MeteringPointType = MeteringPointType.Consumption;
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MeasurementQuality.Calculated)]
    [InlineData(MeasurementQuality.Revised)]
    [InlineData(MeasurementQuality.Estimated)]
    public async Task Consume_MeasurementQualityNotMeasured_NoEventsSaved(MeasurementQuality measurementQuality)
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: measurementQuality);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task Consume_StartDateInTheFuture_NoEventsSaved(int days, int seconds)
    {
        productionMockContract.StartDate = now.AddDays(days).AddSeconds(seconds);

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public async Task Consume_QuantityIsZeroOrNegative_NoEventsSaved(long quantity)
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: quantity,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_MeasurementPeriodAfterIssuingMaxLimit_NoEventsSaved()
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var startDateAfterIssuingMaxLimit = DateTimeOffset.Parse("6200-01-01T00:00:00Z");

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: startDateAfterIssuingMaxLimit.ToUnixTimeSeconds(),
            DateTo: startDateAfterIssuingMaxLimit.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_ValidMeasurement_EventsSaved()
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.Received(1).Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeProduction_NoContract_NoEventsSaved()
    {
        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsNullForAnyArgs();

        var message = new ProductionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeProduction_ConsumptionPoint_NoEventsSaved()
    {
        productionMockContract.MeteringPointType = MeteringPointType.Consumption;
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ProductionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MeasurementQuality.Calculated)]
    [InlineData(MeasurementQuality.Revised)]
    [InlineData(MeasurementQuality.Estimated)]
    public async Task ConsumeProduction_MeasurementQualityNotMeasured_NoEventsSaved(MeasurementQuality measurementQuality)
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ProductionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: measurementQuality);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task ConsumeProduction_StartDateInTheFuture_NoEventsSaved(int days, int seconds)
    {
        productionMockContract.StartDate = now.AddDays(days).AddSeconds(seconds);

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ProductionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public async Task ConsumeProduction_QuantityIsZeroOrNegative_NoEventsSaved(long quantity)
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ProductionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: quantity,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeProduction_MeasurementPeriodAfterIssuingMaxLimit_NoEventsSaved()
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var startDateAfterIssuingMaxLimit = DateTimeOffset.Parse("6200-01-01T00:00:00Z");

        var message = new ProductionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: startDateAfterIssuingMaxLimit.ToUnixTimeSeconds(),
            DateTo: startDateAfterIssuingMaxLimit.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeProduction_ValidMeasurement_EventsSaved()
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { productionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ProductionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await productionRepositoryMock.Received(1).Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeConsumption_NoContract_NoEventsSaved()
    {
        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsNullForAnyArgs();

        var message = new ConsumptionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await consumptionRepositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeConsumption_ProductionPoint_NoEventsSaved()
    {
        consumptionMockContract.MeteringPointType = MeteringPointType.Production;
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { consumptionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ConsumptionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await consumptionRepositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MeasurementQuality.Calculated)]
    [InlineData(MeasurementQuality.Revised)]
    [InlineData(MeasurementQuality.Estimated)]
    public async Task ConsumeConsumption_MeasurementQualityNotMeasured_NoEventsSaved(MeasurementQuality measurementQuality)
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { consumptionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ConsumptionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: measurementQuality);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await consumptionRepositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task ConsumeConsumption_StartDateInTheFuture_NoEventsSaved(int days, int seconds)
    {
        consumptionMockContract.StartDate = now.AddDays(days).AddSeconds(seconds);

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { consumptionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ConsumptionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await consumptionRepositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public async Task ConsumeConsumption_QuantityIsZeroOrNegative_NoEventsSaved(long quantity)
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { consumptionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ConsumptionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: quantity,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await consumptionRepositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeConsumption_MeasurementPeriodAfterIssuingMaxLimit_NoEventsSaved()
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { consumptionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var startDateAfterIssuingMaxLimit = DateTimeOffset.Parse("6200-01-01T00:00:00Z");

        var message = new ConsumptionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: startDateAfterIssuingMaxLimit.ToUnixTimeSeconds(),
            DateTo: startDateAfterIssuingMaxLimit.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await consumptionRepositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeConsumption_ValidMeasurement_EventsSaved()
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { consumptionMockContract });

        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var message = new ConsumptionEnergyMeasuredIntegrationEvent(
            GSRN: productionMockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, productionRepositoryMock, consumptionRepositoryMock, contractServiceMock);

        await consumptionRepositoryMock.Received(1).Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    private static async Task PublishAndConsumeMessage(ProductionEnergyMeasuredIntegrationEvent message,
        IProductionCertificateRepository productionRepository, IConsumptionCertificateRepository consumptionRepository, IContractService contractService)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<EnergyMeasuredEventHandler>())
            .AddSingleton(productionRepository)
            .AddSingleton(consumptionRepository)
            .AddSingleton(contractService)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<ProductionEnergyMeasuredIntegrationEvent>()).Should().BeTrue();
    }

    private static async Task PublishAndConsumeMessage(ConsumptionEnergyMeasuredIntegrationEvent message,
        IProductionCertificateRepository productionRepository, IConsumptionCertificateRepository consumptionRepository, IContractService contractService)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<EnergyMeasuredEventHandler>())
            .AddSingleton(productionRepository)
            .AddSingleton(consumptionRepository)
            .AddSingleton(contractService)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<ConsumptionEnergyMeasuredIntegrationEvent>()).Should().BeTrue();
    }

    private static async Task PublishAndConsumeMessage(EnergyMeasuredIntegrationEvent message,
        IProductionCertificateRepository productionRepository, IConsumptionCertificateRepository consumptionRepository, IContractService contractService)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<EnergyMeasuredEventHandler>())
            .AddSingleton(productionRepository)
            .AddSingleton(consumptionRepository)
            .AddSingleton(contractService)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<EnergyMeasuredIntegrationEvent>()).Should().BeTrue();
    }
}
