using System;
using System.Threading;
using System.Threading.Tasks;
using AggregateRepositories;
using API.GranularCertificateIssuer;
using CertificateEvents.Aggregates;
using CertificateValueObjects;
using Contracts.Certificates;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace API.UnitTests.GranularCertificateIssuer;

public class CertificateRejectedInRegistryEventHandlerTests
{
    [Fact]
    public async void ShouldRejectCertificateAndSave()
    {
        var repositoryMock = new Mock<IProductionCertificateRepository>();

        var cert = new ProductionCertificate("SomeGridArea", new Period(123L, 124L), new Technology("SomeFuelCode", "SomeTechCode"), "SomeMeteringOwner", "571234567890123456", 42);
        repositoryMock
            .Setup(x => x.Get(It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        var msg = new CertificateRejectedInRegistryEvent(cert.Id, "SomeReason");
        await PublishAndConsumeMessage(msg, repositoryMock.Object);

        repositoryMock.Verify(x => x.Save(It.Is<ProductionCertificate>(c => c.IsRejected == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task PublishAndConsumeMessage(CertificateRejectedInRegistryEvent message,
        IProductionCertificateRepository repository)
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg => cfg.AddConsumer<CertificateRejectedInRegistryEventHandler>())
            .AddSingleton(repository)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        await harness.Bus.Publish(message);

        (await harness.Consumed.Any<CertificateRejectedInRegistryEvent>()).Should().BeTrue();
    }
}
