using System;
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

namespace RegistryConnector.Worker.UnitTests
{
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
            var target = fixture
                .WithNotFoundInCache()
                .BuildTarget();

            await target.OnRegistryEvents(new CommandStatusEvent(Some.CommandId, CommandState.Succeeded, null));

            fixture.Verify10CallsToPop();
        }

        [Fact]
        public async void ShouldCallBusPublishOnSuccessfulCachePop()
        {
            await using var provider = new ServiceCollection()
                .AddMassTransitTestHarness()
                .BuildServiceProvider(true);
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            var wrappedEvent = new MessageWrapper<ProductionCertificateCreatedEvent>(Some.ProductionCertificateCreatedEvent, Guid.NewGuid(), Guid.NewGuid());
            var target = fixture
                .WithFoundInCache(wrappedEvent)
                .BuildTarget(harness.Bus);

            await target.OnRegistryEvents(new CommandStatusEvent(Some.CommandId, CommandState.Succeeded, null));

            harness.Published.Count().Should().Be(1);
            fixture.VerifyOneCallToCachePop();
        }

        private class PoRegistryEventHandlerFixture
        {
            private readonly Mock<ILogger<ProjectOriginRegistryEventHandler>> loggerMock;
            private readonly Mock<ICertificateEventsInMemoryCache> cacheMock;
            private readonly Mock<IBus> busMock;

            public PoRegistryEventHandlerFixture()
            {
                loggerMock = new Mock<ILogger<ProjectOriginRegistryEventHandler>>();
                cacheMock = new Mock<ICertificateEventsInMemoryCache>();
                busMock = new Mock<IBus>();
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

            public ProjectOriginRegistryEventHandler BuildTarget() => new(loggerMock.Object, cacheMock.Object, busMock.Object);
            public ProjectOriginRegistryEventHandler BuildTarget(IBus bus) => new(loggerMock.Object, cacheMock.Object, bus);

            public void Verify10CallsToPop()
            {
                cacheMock.Verify(x => x.PopCertificateWithCommandId(It.IsAny<CommandId>()), Times.Exactly(10));
            }

            public void VerifyOneCallToCachePop()
            {
                cacheMock.Verify(x => x.PopCertificateWithCommandId(It.IsAny<CommandId>()), Times.Once);
            }

        }
    }
}
