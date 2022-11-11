using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class IssuerWorker : BackgroundService
{
    private readonly IEventStore eventStore;
    private readonly IEnergyMeasuredEventHandler eventHandler;
    private readonly ILogger<IssuerWorker> logger;

    public IssuerWorker(IEventStore eventStore, IEnergyMeasuredEventHandler energyMeasuredEventHandler, ILogger<IssuerWorker> logger)
    {
        this.logger = logger;
        this.eventStore = eventStore;
        eventHandler = energyMeasuredEventHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = eventStore
            .GetBuilder(Topic.MeasurementPrefix)
            .AddHandler<EnergyMeasured>(e =>
            {
                logger.LogInformation("GranularCertificateIssuer received: {event}", e.EventModel);

                var handleTask = eventHandler.Handle(e.EventModel);
                var productionCertificateCreatedEvent = handleTask.GetAwaiter().GetResult(); // Forced to do blocking call here. IEventConsumerBuilder does not currently support async handlers

                //if (productionCertificateCreatedEvent != null)
                //{
                //    var topic = Topic.For(productionCertificateCreatedEvent);
                //    var produceTask = eventStore.Produce(productionCertificateCreatedEvent, topic);
                //    produceTask.GetAwaiter().GetResult(); // Forced to do blocking call here. IEventConsumerBuilder does not currently support async handlers
                //}
            })
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
