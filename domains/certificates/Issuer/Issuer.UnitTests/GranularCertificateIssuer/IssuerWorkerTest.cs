using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using EnergyOriginEventStore.EventStore.Serialization;
using Issuer.Worker.GranularCertificateIssuer;
using Issuer.Worker.MasterDataService;
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
        var semaphore = new SemaphoreSlim(0);
        
        var worker = new IssuerWorker(eventStore, Mock.Of<IMasterDataService>(), Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(CancellationToken.None);
        
        var @event = new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        await eventStore.Produce(@event, Topic.For(@event));

        ProductionCertificateCreated? producedEvent = null;

        using var consumer = eventStore
            .GetBuilder(Topic.CertificatePrefix)
            .AddHandler<ProductionCertificateCreated>(e =>
            {
                producedEvent = e.EventModel;
                semaphore.Release();
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.NotNull(producedEvent);
    }

    [Fact]
    public async Task Prod2()
    {
        using IEventStore eventStore = new MemoryEventStore();

        var worker = new IssuerWorker(eventStore, Mock.Of<IMasterDataService>(), Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(CancellationToken.None);

        var @event = new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured);
        var producedEvent = await eventStore.Test<ProductionCertificateCreated>(@event, Topic.For(@event), Topic.CertificatePrefix);

        Assert.NotNull(producedEvent);
    }
}

internal static class EventStoreExtensions
{
    public static async Task<T> Test<T>(this IEventStore eventStore, EventModel @event, string topic, string topicPrefix) where T : EventModel
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

        var success = await semaphore.WaitAsync(TimeSpan.FromSeconds(1)); //This makes it really hard to test when there is an incoming event, but not outgoing event.
        if (!success)
        {
            throw new Exception("Timeout");
        }

        if (producedEvent == null)
        {
            throw new Exception("No produced event");
        }

        return producedEvent;
    }
}
