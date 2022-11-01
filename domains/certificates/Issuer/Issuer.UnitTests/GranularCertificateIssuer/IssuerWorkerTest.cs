using System;
using System.Numerics;
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
            .ReturnsAsync(value: null);

        using var worker = new IssuerWorker(eventStore, eventHandlerMock.Object, Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(cancellationToken: CancellationToken.None);

        var energyMeasuredEvent = new EnergyMeasured(
            GSRN: "gsrn",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        var producedEvent = await GetProducedEvent<ProductionCertificateCreated>(eventStore, energyMeasuredEvent, Topic.For(energyMeasuredEvent), Topic.CertificatePrefix);

        Assert.Null(producedEvent);
    }

    [Fact]
    public async Task Worker_EventFromHandler_ProducesEventToEventStore()
    {
        using IEventStore eventStore = new MemoryEventStore();

        var productionCertificateCreated = new ProductionCertificateCreated(
            CertificateId: Guid.NewGuid(),
            GridArea: "gridArea",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Technology: new Technology(FuelCode: "F00000000", TechCode: "T010000"),
            MeteringPointOwner: "meteringPointOwner",
            ShieldedGSRN: new ShieldedValue<string>(Value: "gsrn", R: BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(Value: 42, R: BigInteger.Zero));

        var eventHandlerMock = new Mock<IEnergyMeasuredEventHandler>();
        eventHandlerMock
            .Setup(m => m.Handle(It.IsAny<EnergyMeasured>()))
            .ReturnsAsync(productionCertificateCreated);

        using var worker = new IssuerWorker(eventStore, eventHandlerMock.Object, Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(cancellationToken: CancellationToken.None);

        var @event = new EnergyMeasured(
            GSRN: "gsrn",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

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
