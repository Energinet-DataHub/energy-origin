using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Issuer.Worker.RegistryConnector;

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
            .GetBuilder(Topics.CertificatePrefix)
            .AddHandler<CertificateCreated>(e =>
            {
                logger.LogInformation("RegistryConnectorWorker received: {event}", e.EventModel);

                var @event = new CertificateIssued(e.EventModel.CertificateId);

                var produceTask = eventStore.Produce(@event, Topics.Certificate(@event.CertificateId.ToString()));
                produceTask.GetAwaiter().GetResult(); // IEventConsumerBuilder does not currently support async handlers
            })
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
