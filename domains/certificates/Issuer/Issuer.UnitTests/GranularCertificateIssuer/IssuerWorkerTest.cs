using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using EnergyOriginEventStore.EventStore.Serialization;
using Issuer.Worker.GranularCertificateIssuer;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Issuer.UnitTests.GranularCertificateIssuer;

public class IssuerWorkerTest
{
    [Fact]
    public async Task Worker_NoEventFromHandler_NoEventProduced()
    {
        using IEventStore eventStore = new MemoryEventStore();

        var eventHandlerMock = new Mock<IEnergyMeasuredEventHandler>();
        eventHandlerMock
            .Setup(m => m.Handle(It.IsAny<EnergyMeasured>()))
            .ReturnsAsync(null as ProductionCertificateCreated);

        using var worker = new IssuerWorker(eventStore, eventHandlerMock.Object, Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(CancellationToken.None);

        var energyMeasuredEvent = new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await GetProducedEvent<ProductionCertificateCreated>(eventStore, energyMeasuredEvent, Topic.For(energyMeasuredEvent), Topic.CertificatePrefix);

        Assert.Null(producedEvent);
    }

    [Fact]
    public async Task Worker_EventFromHandler_ProducesEventToEventStore()
    {
        using IEventStore eventStore = new MemoryEventStore();

        var productionCertificateCreated = new ProductionCertificateCreated(Guid.NewGuid(), "gridArea", new Period(1, 42),
            new Technology("F00000000", "T010000"), "meteringPointOwner", new ShieldedValue<string>("gsrn", 42),
            new ShieldedValue<long>(42, 42));

        var eventHandlerMock = new Mock<IEnergyMeasuredEventHandler>();
        eventHandlerMock
            .Setup(m => m.Handle(It.IsAny<EnergyMeasured>()))
            .ReturnsAsync(productionCertificateCreated);

        using var worker = new IssuerWorker(eventStore, eventHandlerMock.Object, Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(CancellationToken.None);

        var @event = new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await GetProducedEvent<ProductionCertificateCreated>(eventStore, @event, Topic.For(@event), Topic.CertificatePrefix);

        Assert.NotNull(producedEvent);
    }
    
    [Fact]
    public async Task Worker_CancellationTokenCancelled_WorkerCompletes()
    {
        using IEventStore eventStore = new MemoryEventStore();

        using var worker = new IssuerWorker(eventStore, Mock.Of<IEnergyMeasuredEventHandler>(), Mock.Of<ILogger<IssuerWorker>>());

        var tokenSource = new CancellationTokenSource();
        
        await worker.StartAsync(tokenSource.Token);

        Assert.False(worker.ExecuteTask.IsCompleted);

        tokenSource.Cancel();

        Assert.True(worker.ExecuteTask.IsCompleted);
    }

    private static async Task<T?> GetProducedEvent<T>(IEventStore eventStore, EventModel @event, string topic, string topicPrefix) where T : EventModel
    {
        var semaphore = new SemaphoreSlim(0);

        await eventStore.Produce(@event, topic);

        T? producedEvent = null;

        using var consumer = eventStore
            .GetBuilder(topicPrefix)
            .AddHandler<T>(e =>
            {
                producedEvent = e.EventModel;
                semaphore.Release();
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(100));

        return producedEvent;
    }
}
