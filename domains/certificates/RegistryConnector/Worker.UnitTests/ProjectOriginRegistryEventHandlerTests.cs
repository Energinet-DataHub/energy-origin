using System;
using System.Threading.Tasks;
using Contracts.Certificates;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectOrigin.Electricity.Client.Models;
using RegistryConnector.Worker.Cache;
using RegistryConnector.Worker.EventHandlers;
using Xunit;

namespace RegistryConnector.Worker.UnitTests;

public class ProjectOriginRegistryEventHandlerTests
{
    private readonly PoRegistryEventHandlerFixture fixture;

    public ProjectOriginRegistryEventHandlerTests()
    {
        fixture = new PoRegistryEventHandlerFixture();
    }

    [Fact]
    public async void ShouldRetryCachePop10Times()
    {
        var target = await fixture
            .WithNotFoundInCache()
            .BuildTargetAsync();

        await target.OnRegistryEvents(new CommandStatusEvent(Some.CommandId, CommandState.Succeeded, null));

        fixture.Verify10CallsToPop();
    }

    [Fact]
    public async void ShouldCallBusPublishOnSuccessfulCachePop()
    {
        var wrappedEvent = new MessageWrapper<ProductionCertificateCreatedEvent>(Some.ProductionCertificateCreatedEvent, Guid.NewGuid(), Guid.NewGuid());
        var target = await fixture
            .WithFoundInCache(wrappedEvent)
            .BuildTargetAsync();

        await target.OnRegistryEvents(new CommandStatusEvent(Some.CommandId, CommandState.Succeeded, null));

        fixture.VerifyBusCallOnce();
        fixture.VerifyOneCallToCachePop();
    }

    private class PoRegistryEventHandlerFixture
    {
        private readonly Mock<ILogger<ProjectOriginRegistryEventHandler>> loggerMock;
        private readonly Mock<ICertificateEventsInMemoryCache> cacheMock;
        private ITestHarness? harness;

        public PoRegistryEventHandlerFixture()
        {
            loggerMock = new Mock<ILogger<ProjectOriginRegistryEventHandler>>();
            cacheMock = new Mock<ICertificateEventsInMemoryCache>();
        }

        public PoRegistryEventHandlerFixture WithNotFoundInCache()
        {
            cacheMock.Setup(x => x.PopCertificateWithCommandId(It.IsAny<CommandId>())).Returns(() => null);
            return this;
        }

        public PoRegistryEventHandlerFixture WithFoundInCache(MessageWrapper<ProductionCertificateCreatedEvent> @event)
        {
            cacheMock.Setup(x => x.PopCertificateWithCommandId(It.IsAny<CommandId>())).Returns(@event);
            return this;
        }

        public async Task<ProjectOriginRegistryEventHandler> BuildTargetAsync()
        {
            await using var provider = new ServiceCollection()
                .AddMassTransitTestHarness()
                .BuildServiceProvider(true);
            harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();
            return new(loggerMock.Object, cacheMock.Object, harness.Bus);
        }

        public void Verify10CallsToPop()
        {
            cacheMock.Verify(x => x.PopCertificateWithCommandId(It.IsAny<CommandId>()), Times.Exactly(10));
        }

        public void VerifyOneCallToCachePop()
        {
            cacheMock.Verify(x => x.PopCertificateWithCommandId(It.IsAny<CommandId>()), Times.Once);
        }

        public void VerifyBusCallOnce()
        {
            harness.Published.Count().Should().Be(1);
        }
    }
}
