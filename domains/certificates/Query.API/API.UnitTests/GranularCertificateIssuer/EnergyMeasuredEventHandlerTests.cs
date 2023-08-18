using System;
using System.Threading;
using System.Threading.Tasks;
using AggregateRepositories;
using API.ContractService;
using API.GranularCertificateIssuer;
using CertificateEvents.Aggregates;
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
        var repositoryMock = Substitute.For<IProductionCertificateRepository>();

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
    }

    [Fact]
    public async Task Consume_ConsumptionPoint_NoEventsSaved()
    {
        mockContract.MeteringPointType = MeteringPointType.Consumption;
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MeasurementQuality.Calculated)]
    [InlineData(MeasurementQuality.Revised)]
    [InlineData(MeasurementQuality.Estimated)]
    public async Task Consume_MeasurementQualityNotMeasured_NoEventsSaved(MeasurementQuality measurementQuality)
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: measurementQuality);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task Consume_StartDateInTheFuture_NoEventsSaved(int days, int seconds)
    {
        mockContract.StartDate = now.AddDays(days).AddSeconds(seconds);

        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public async Task Consume_QuantityIsZeroOrNegative_NoEventsSaved(long quantity)
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: quantity,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_MeasurementPeriodOutsideScope_NoEventsSaved()
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: DateTimeOffset.Parse("6200-01-01T00:00:00Z").ToUnixTimeSeconds(),
            DateTo: DateTimeOffset.Parse("6200-01-01T01:00:00Z").ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.DidNotReceive().Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_ValidMeasurement_EventsSaved()
    {
        var contractServiceMock = Substitute.For<IContractService>();
        contractServiceMock.GetByGSRN(string.Empty, default).ReturnsForAnyArgs(new[] { mockContract });

        var repositoryMock = Substitute.For<IProductionCertificateRepository>();

        var message = new EnergyMeasuredIntegrationEvent(
            GSRN: mockContract.GSRN,
            DateFrom: now.ToUnixTimeSeconds(),
            DateTo: now.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await PublishAndConsumeMessage(message, repositoryMock, contractServiceMock);

        await repositoryMock.Received(1).Save(Arg.Any<ProductionCertificate>(), Arg.Any<CancellationToken>());
    }

    private static async Task PublishAndConsumeMessage(EnergyMeasuredIntegrationEvent message,
        IProductionCertificateRepository repository, IContractService contractService)
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
