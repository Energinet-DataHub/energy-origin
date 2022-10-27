using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Serialization;
using Issuer.Worker.GranularCertificateIssuer;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Issuer.UnitTests.GranualarCertificateIssuer;

public class IssuerWorkerTest
{
    [Fact]
    public async Task Prod1()
    {
        var logger = Mock.Of<ILogger<IssuerWorker>>();
        var eventStoreMock = new Mock<IEventStore>();

        var worker = new IssuerWorker(logger, eventStoreMock.Object);

        await worker.StartAsync(CancellationToken.None);

        var consumerBuilderMock = new Mock<IEventConsumerBuilder>();
        eventStoreMock.Setup(m => m.GetBuilder(It.IsAny<string>())).Returns(consumerBuilderMock.Object);

        consumerBuilderMock.Setup(m => m.AddHandler(It.IsAny<Action<Event<EnergyMeasured>>>()))
            .Callback(Callback);
    }

    private void Callback(Action<Event<EnergyMeasured>> action)
    {
        var @event = new Event<EnergyMeasured>(new EnergyMeasured("gsrn", new Period(1, 42), 42, EnergyMeasurementQuality.Measured), "1");
        action(@event);

        throw new NotImplementedException();
    }

    private class MyEvent<T> : Event<T> where T : EnergyOriginEventStore.EventStore.Serialization.EventModel
    {

    }
}
