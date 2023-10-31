using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.GranularCertificateIssuer;
using CertificateValueObjects;
using Contracts.Certificates.CertificateRejectedInRegistry.V1;
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
    public async Task ShouldRejectCertificateAndSave_WhenProductionCertificate()
    {
        var repositoryMock = Substitute.For<ICertificateRepository>();

        var cert = new ProductionCertificate("SomeGridArea", new Period(123L, 124L), new Technology("SomeFuelCode", "SomeTechCode"), "SomeMeteringOwner", "571234567890123456", 42, Array.Empty<byte>());
        repositoryMock.GetProductionCertificate(default).ReturnsForAnyArgs(cert);

        var msg = new Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent(cert.Id, MeteringPointType.Production, "SomeReason");
        await PublishAndConsumeMessage(msg, repositoryMock);

        await repositoryMock.Received(1).Save(Arg.Is<ProductionCertificate>(c => c.IsRejected == true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldRejectCertificateAndSave_WhenConsumptionCertificate()
    {
        var repositoryMock = Substitute.For<ICertificateRepository>();

        var cert = new ConsumptionCertificate("SomeGridArea", new Period(123L, 124L), "SomeMeteringOwner", "571234567890123456", 42, Array.Empty<byte>());
        repositoryMock.GetConsumptionCertificate(default).ReturnsForAnyArgs(cert);

        var msg = new Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent(cert.Id, MeteringPointType.Consumption, "SomeReason");
        await PublishAndConsumeMessage(msg, repositoryMock);

        await repositoryMock.Received(1).Save(Arg.Is<ConsumptionCertificate>(c => c.IsRejected == true), Arg.Any<CancellationToken>());
    }

    private static async Task PublishAndConsumeMessage(CertificateRejectedInRegistryEvent message,
        ICertificateRepository repository)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<CertificateRejectedInRegistryEventHandler>())
            .AddSingleton(repository)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent>()).Should().BeTrue();
    }
}
