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
    private readonly ILogger<IssuerWorker> _logger;
    private readonly IEventStore _eventStore;

    public IssuerWorker(ILogger<IssuerWorker> logger, IEventStore eventStore)
    {
        _logger = logger;
        _eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = _eventStore
            .GetBuilder(Topic.MeasurementPrefix)
            .AddHandler<EnergyMeasured>(e =>
            {
                _logger.LogInformation("GranularCertificateIssuer received: {event}", e.EventModel);

                var @event = new CertificateCreated(
                    Guid.NewGuid(),
                    "gridArea",
                    e.EventModel.Period,
                    new("fuel", "tech"),
                    Array.Empty<byte>(),
                    new ShieldedValue<string>(e.EventModel.GSRN, 42),
                    new ShieldedValue<long>(e.EventModel.Quantity, 42));

                var produceTask = _eventStore.Produce(@event, Topic.For(@event));
                produceTask.GetAwaiter().GetResult(); // IEventConsumerBuilder does not currently support async handlers
            })
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
