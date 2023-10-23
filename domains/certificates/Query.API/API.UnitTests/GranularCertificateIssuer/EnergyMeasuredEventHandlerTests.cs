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

    private readonly CertificateIssuingContract mockContract = new()
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

    [Fact]
    public async Task Consume_NoContract_NoEventsSaved()
    {
        var repositoryMock = Substitute.For<ICertificateRepository>();

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsNullForAnyArgs();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
        await repositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MeasurementQuality.Calculated, MeteringPointType.Production)]
    [InlineData(MeasurementQuality.Revised, MeteringPointType.Production)]
    [InlineData(MeasurementQuality.Estimated, MeteringPointType.Production)]
    [InlineData(MeasurementQuality.Calculated, MeteringPointType.Consumption)]
    [InlineData(MeasurementQuality.Revised, MeteringPointType.Consumption)]
    [InlineData(MeasurementQuality.Estimated, MeteringPointType.Consumption)]
    public async Task Consume_MeasurementQualityNotMeasured_NoEventsSaved(MeasurementQuality measurementQuality, MeteringPointType meteringPointType)
    {
        mockContract.MeteringPointType = meteringPointType;
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<ICertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: measurementQuality);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
        await repositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 0, MeteringPointType.Production)]
    [InlineData(0, 1, MeteringPointType.Production)]
    [InlineData(1, 0, MeteringPointType.Consumption)]
    [InlineData(0, 1, MeteringPointType.Consumption)]
    public async Task Consume_StartDateInTheFuture_NoEventsSaved(int days, int seconds, MeteringPointType meteringPointType)
    {
        mockContract.MeteringPointType = meteringPointType;
        mockContract.StartDate = now.AddDays(days).AddSeconds(seconds);

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<ICertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
        await repositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, MeteringPointType.Production)]
    [InlineData(-42, MeteringPointType.Production)]
    [InlineData(0, MeteringPointType.Consumption)]
    [InlineData(-42, MeteringPointType.Consumption)]
    public async Task Consume_QuantityIsZeroOrNegative_NoEventsSaved(long quantity, MeteringPointType meteringPointType)
    {
        mockContract.MeteringPointType = meteringPointType;
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<ICertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: quantity,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
        await repositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MeteringPointType.Production)]
    [InlineData(MeteringPointType.Consumption)]
    public async Task Consume_MeasurementPeriodAfterIssuingMaxLimit_NoEventsSaved(MeteringPointType meteringPointType)
    {
        mockContract.MeteringPointType = meteringPointType;
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<ICertificateRepository>();

        var startDateAfterIssuingMaxLimit = DateTimeOffset.Parse("6200-01-01T00:00:00Z");

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: startDateAfterIssuingMaxLimit.ToUnixTimeSeconds(),
            DateTo: startDateAfterIssuingMaxLimit.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
        await repositoryMock.DidNotReceive().Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MeteringPointType.Production)]
    [InlineData(MeteringPointType.Consumption)]
    public async Task Consume_ValidMeasurement_EventsSaved(MeteringPointType meteringPointType)
    {
        mockContract.MeteringPointType = meteringPointType;
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<ICertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        if (meteringPointType == MeteringPointType.Production)
        {
            await repositoryMock.Received(1).Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
        }
        if (meteringPointType == MeteringPointType.Consumption)
        {
            await repositoryMock.Received(1).Save(Arg.Any<ConsumptionCertificate>(), Arg.Any<CancellationToken>());
        }
    }

    private static async Task PublishAndConsumeMessage(EnergyMeasuredIntegrationEvent message,
        ICertificateRepository repository, IContractService contractService)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<EnergyMeasuredEventHandler>())
            .AddSingleton(repository)
            .AddSingleton(contractService)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<EnergyMeasuredIntegrationEvent>()).Should().BeTrue();
    }
}
