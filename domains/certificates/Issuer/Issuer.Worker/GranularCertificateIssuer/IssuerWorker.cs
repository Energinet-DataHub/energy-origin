using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using CertificateEvents.Primitives;
using EnergyOriginEventStore.EventStore;
using Issuer.Worker.MasterDataService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Issuer.Worker.GranularCertificateIssuer;

public class IssuerWorker : BackgroundService
{
    private readonly IEventStore eventStore;
    private readonly IMasterDataService masterDataService;
    private readonly ILogger<IssuerWorker> logger;

    public IssuerWorker(IEventStore eventStore, IMasterDataService masterDataService, ILogger<IssuerWorker> logger)
    {
        this.logger = logger;
        this.eventStore = eventStore;
        this.masterDataService = masterDataService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = eventStore
            .GetBuilder(Topic.MeasurementPrefix)
            .AddHandler<EnergyMeasured>(e =>
            {
                logger.LogInformation("GranularCertificateIssuer received: {event}", e.EventModel);

                var @event = new ProductionCertificateCreated(
                    Guid.NewGuid(),
                    "gridArea",
                    e.EventModel.Period,
                    new("fuel", "tech"),
                    "foo",
                    new ShieldedValue<string>(e.EventModel.GSRN, 42),
                    new ShieldedValue<long>(e.EventModel.Quantity, 42));

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
