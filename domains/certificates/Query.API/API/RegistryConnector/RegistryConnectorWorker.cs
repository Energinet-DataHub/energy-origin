using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.RegistryConnector;

public class RegistryConnectorWorker : BackgroundService
{
    private readonly ILogger<RegistryConnectorWorker> logger;
    private readonly IEventStore eventStore;

    public RegistryConnectorWorker(ILogger<RegistryConnectorWorker> logger, IEventStore eventStore)
    {
        this.logger = logger;
        this.eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = eventStore
            .GetBuilder(Topic.CertificatePrefix)
            .AddHandler<ProductionCertificateCreated>(e =>
            {
                logger.LogInformation("RegistryConnectorWorker received: {event}", e.EventModel);

                var @event = new ProductionCertificateIssued(e.EventModel.CertificateId);

                var produceTask = eventStore.Produce(@event, Topic.For(@event));
                produceTask.GetAwaiter().GetResult(); // IEventConsumerBuilder does not currently support async handlers
            })
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
