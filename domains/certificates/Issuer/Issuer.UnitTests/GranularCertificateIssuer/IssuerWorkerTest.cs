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
    public async Task Prod1()
    {
        using IEventStore eventStore = new MemoryEventStore();

        var eventHandlerMock = new Mock<IEnergyMeasuredEventHandler>();
        eventHandlerMock
            .Setup(m => m.Handle(It.IsAny<EnergyMeasured>()))
            .ReturnsAsync(null as ProductionCertificateCreated);

        var worker = new IssuerWorker(eventStore, eventHandlerMock.Object, Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(CancellationToken.None);

        var @event = new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await eventStore.Test<ProductionCertificateCreated>(@event, Topic.For(@event), Topic.CertificatePrefix);

        Assert.Null(producedEvent);
    }

    [Fact]
    public async Task Prod2()
    {
        using IEventStore eventStore = new MemoryEventStore();

        var eventHandlerMock = new Mock<IEnergyMeasuredEventHandler>();
        eventHandlerMock
            .Setup(m => m.Handle(It.IsAny<EnergyMeasured>()))
            .ReturnsAsync(null as ProductionCertificateCreated);

        var worker = new IssuerWorker(eventStore, eventHandlerMock.Object, Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(CancellationToken.None);

        var @event = new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await eventStore.Test<ProductionCertificateCreated>(@event, Topic.For(@event), Topic.CertificatePrefix);

        Assert.Null(producedEvent);
        Assert.False(true);
    }
}

internal static class EventStoreExtensions
{
    public static async Task<T?> Test<T>(this IEventStore eventStore, EventModel @event, string topic, string topicPrefix) where T : EventModel
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

        await semaphore.WaitAsync(TimeSpan.FromSeconds(1));

        return producedEvent;
    }
}
