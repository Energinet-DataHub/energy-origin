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
            .Setup(expression: m => m.Handle(It.IsAny<EnergyMeasured>()))
            .ReturnsAsync(value: null as ProductionCertificateCreated);

        using var worker = new IssuerWorker(eventStore: eventStore, energyMeasuredEventHandler: eventHandlerMock.Object, logger: Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(cancellationToken: CancellationToken.None);

        var energyMeasuredEvent = new EnergyMeasured(
            GSRN: "gsrn",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        var producedEvent = await GetProducedEvent<ProductionCertificateCreated>(eventStore: eventStore, @event: energyMeasuredEvent, topic: Topic.For(@event: energyMeasuredEvent), topicPrefix: Topic.CertificatePrefix);

        Assert.Null(@object: producedEvent);
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
            .Setup(expression: m => m.Handle(It.IsAny<EnergyMeasured>()))
            .ReturnsAsync(value: productionCertificateCreated);

        using var worker = new IssuerWorker(eventStore: eventStore, energyMeasuredEventHandler: eventHandlerMock.Object, logger: Mock.Of<ILogger<IssuerWorker>>());

        await worker.StartAsync(cancellationToken: CancellationToken.None);

        var @event = new EnergyMeasured(
            GSRN: "gsrn",
            Period: new Period(DateFrom: 1, DateTo: 42),
            Quantity: 42,
            Quality: EnergyMeasurementQuality.Measured);

        var producedEvent = await GetProducedEvent<ProductionCertificateCreated>(eventStore: eventStore, @event: @event, topic: Topic.For(@event: @event), topicPrefix: Topic.CertificatePrefix);

        Assert.NotNull(@object: producedEvent);
    }

    [Fact]
    public async Task Worker_CancellationTokenCancelled_WorkerCompletes()
    {
        using IEventStore eventStore = new MemoryEventStore();

        using var worker = new IssuerWorker(eventStore: eventStore, energyMeasuredEventHandler: Mock.Of<IEnergyMeasuredEventHandler>(), logger: Mock.Of<ILogger<IssuerWorker>>());

        var tokenSource = new CancellationTokenSource();

        await worker.StartAsync(cancellationToken: tokenSource.Token);

        Assert.False(condition: worker.ExecuteTask.IsCompleted);

        tokenSource.Cancel();

        Assert.True(condition: worker.ExecuteTask.IsCompleted);
    }

    private static async Task<T?> GetProducedEvent<T>(IEventStore eventStore, EventModel @event, string topic, string topicPrefix) where T : EventModel
    {
        var semaphore = new SemaphoreSlim(initialCount: 0);

        await eventStore.Produce(model: @event, topic);

        T? producedEvent = null;

        using var consumer = eventStore
            .GetBuilder(topicPrefix: topicPrefix)
            .AddHandler<T>(handler: e =>
            {
                producedEvent = e.EventModel;
                semaphore.Release();
            })
            .Build();

        await semaphore.WaitAsync(timeout: TimeSpan.FromMilliseconds(value: 100));

        return producedEvent;
    }
}
