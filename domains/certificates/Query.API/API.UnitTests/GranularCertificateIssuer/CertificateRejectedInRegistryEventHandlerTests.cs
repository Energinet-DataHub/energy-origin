using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.GranularCertificateIssuer;
using CertificateValueObjects;
using Contracts.Certificates;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace API.UnitTests.GranularCertificateIssuer;

public class CertificateRejectedInRegistryEventHandlerTests
{
    [Fact]
    public async Task ShouldRejectCertificateAndSave()
    {
        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var cert = new ProductionCertificate("SomeGridArea", new Period(123L, 124L), new Technology("SomeFuelCode", "SomeTechCode"), "SomeMeteringOwner", "571234567890123456", 42, Array.Empty<byte>());
        productionRepositoryMock.Get(default).ReturnsForAnyArgs(cert);

        var msg = new CertificateRejectedInRegistryEvent(cert.Id, "SomeReason");
        await PublishAndConsumeMessage(msg, productionRepositoryMock, consumptionRepositoryMock);

        await productionRepositoryMock.Received(1).Save(Arg.Is<ProductionCertificate>(c => c.IsRejected == true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldRejectCertificateAndSave_WhenProductionCertificate()
    {
        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var cert = new ProductionCertificate("SomeGridArea", new Period(123L, 124L), new Technology("SomeFuelCode", "SomeTechCode"), "SomeMeteringOwner", "571234567890123456", 42, Array.Empty<byte>());
        productionRepositoryMock.Get(default).ReturnsForAnyArgs(cert);

        var msg = new Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent(cert.Id, MeteringPointType.Production, "SomeReason");
        await PublishAndConsumeMessage(msg, productionRepositoryMock, consumptionRepositoryMock);

        await productionRepositoryMock.Received(1).Save(Arg.Is<ProductionCertificate>(c => c.IsRejected == true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldRejectCertificateAndSave_WhenConsumptionCertificate()
    {
        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var cert = new ConsumptionCertificate("SomeGridArea", new Period(123L, 124L), "SomeMeteringOwner", "571234567890123456", 42, Array.Empty<byte>());
        consumptionRepositoryMock.Get(default).ReturnsForAnyArgs(cert);

        var msg = new Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent(cert.Id, MeteringPointType.Consumption, "SomeReason");
        await PublishAndConsumeMessage(msg, productionRepositoryMock, consumptionRepositoryMock);

        await consumptionRepositoryMock.Received(1).Save(Arg.Is<ConsumptionCertificate>(c => c.IsRejected == true), Arg.Any<CancellationToken>());
    }

    private static async Task PublishAndConsumeMessage(CertificateRejectedInRegistryEvent message,
        IProductionCertificateRepository productionRepository, IConsumptionCertificateRepository consumptionRepository)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<CertificateRejectedInRegistryEventHandler>())
            .AddSingleton(productionRepository)
            .AddSingleton(consumptionRepository)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<CertificateRejectedInRegistryEvent>()).Should().BeTrue();
    }

    private static async Task PublishAndConsumeMessage(Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent message,
        IProductionCertificateRepository productionRepository, IConsumptionCertificateRepository consumptionRepository)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<CertificateRejectedInRegistryEventHandler>())
            .AddSingleton(productionRepository)
            .AddSingleton(consumptionRepository)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent>()).Should().BeTrue();
    }
}
