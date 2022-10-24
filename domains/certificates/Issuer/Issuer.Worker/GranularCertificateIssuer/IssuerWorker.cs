using System;
using System.Threading;
using System.Threading.Tasks;
using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Serialization;
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
            .GetBuilder("topic1")
            .AddHandler<SomethingHappened>(Handler)
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            //logger.LogInformation("Worker Tick");
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private void Handler(Event<SomethingHappened> e)
    {
        logger.LogInformation($"Received: {e.EventModel.Foo}");

        var produceTask = eventStore.Produce(new ThenThisHappened("bar bar"), "topic2");
        produceTask.GetAwaiter().GetResult(); // IEventConsumerBuilder does not currently support async handlers
    }
}
