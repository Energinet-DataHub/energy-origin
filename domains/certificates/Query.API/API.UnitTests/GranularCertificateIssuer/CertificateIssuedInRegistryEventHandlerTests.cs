using System;
using API.Data;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using API.GranularCertificateIssuer;
using FluentAssertions;
using MassTransit;
using CertificateValueObjects;
using NSubstitute;
using System.Threading;
using Xunit;

namespace API.UnitTests.GranularCertificateIssuer;

public class CertificateIssuedInRegistryEventHandlerTests
{
    [Fact]
    public async Task ShouldIssueCertificateAndSave_WhenProductionCertificate()
    {
        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var cert = new ProductionCertificate("SomeGridArea", new Period(123L, 124L), new Technology("SomeFuelCode", "SomeTechCode"), "SomeMeteringOwner", "571234567890123456", 42, Array.Empty<byte>());
        productionRepositoryMock.Get(default).ReturnsForAnyArgs(cert);

        var msg = new Contracts.Certificates.CertificateIssuedInRegistry.V1.CertificateIssuedInRegistryEvent(cert.Id, "SomeRegistry", new byte[1], cert.Quantity, MeteringPointType.Production, new byte[1], "https://foo", 43);
        await PublishAndConsumeMessage(msg, productionRepositoryMock, consumptionRepositoryMock);

        await productionRepositoryMock.Received(1).Save(Arg.Is<ProductionCertificate>(c => c.IsIssued == true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldIssueCertificateAndSave_WhenConsumptionCertificate()
    {
        var productionRepositoryMock = Substitute.For<IProductionCertificateRepository>();
        var consumptionRepositoryMock = Substitute.For<IConsumptionCertificateRepository>();

        var cert = new ConsumptionCertificate("SomeGridArea", new Period(123L, 124L), "SomeMeteringOwner", "571234567890123456", 42, Array.Empty<byte>());
        consumptionRepositoryMock.Get(default).ReturnsForAnyArgs(cert);

        var msg = new Contracts.Certificates.CertificateIssuedInRegistry.V1.CertificateIssuedInRegistryEvent(cert.Id, "SomeRegistry", new byte[1], cert.Quantity, MeteringPointType.Consumption, new byte[1], "https://foo", 43);
        await PublishAndConsumeMessage(msg, productionRepositoryMock, consumptionRepositoryMock);

        await consumptionRepositoryMock.Received(1).Save(Arg.Is<ConsumptionCertificate>(c => c.IsIssued == true), Arg.Any<CancellationToken>());
    }

    private static async Task PublishAndConsumeMessage(Contracts.Certificates.CertificateIssuedInRegistry.V1.CertificateIssuedInRegistryEvent message,
        IProductionCertificateRepository productionRepository, IConsumptionCertificateRepository consumptionRepository)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<CertificateIssuedInRegistryEventHandler>())
            .AddSingleton(productionRepository)
            .AddSingleton(consumptionRepository)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<Contracts.Certificates.CertificateIssuedInRegistry.V1.CertificateIssuedInRegistryEvent>()).Should().BeTrue();
    }
}
