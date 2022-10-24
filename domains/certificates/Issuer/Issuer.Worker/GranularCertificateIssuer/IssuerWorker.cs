using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Issuer.Worker.GranularCertificateIssuer;

public class IssuerWorker : BackgroundService
{
    private readonly ILogger<IssuerWorker> logger;
    private readonly IEventStore eventStore;

    public IssuerWorker(ILogger<IssuerWorker> logger, IEventStore eventStore)
    {
        this.logger = logger;
        this.eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = eventStore
            .GetBuilder(Topics.MeasurementPrefix)
            .AddHandler<EnergyMeasured>(e =>
            {
                logger.LogInformation("GranularCertificateIssuer received: {event}", e.EventModel);

                var @event = new CertificateCreated(
                    Guid.NewGuid(),
                    "gridArea",
                    e.EventModel.Period,
                    new("fuel", "tech"),
                    Array.Empty<byte>(),
                    new ShieldedValue<string>(e.EventModel.GSRN, 42),
                    new ShieldedValue<long>(e.EventModel.Quantity, 42));

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
