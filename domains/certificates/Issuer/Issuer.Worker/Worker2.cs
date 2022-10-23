using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Serialization;
using Issuer.Worker;

public class Worker2 : BackgroundService
{
    private readonly ILogger<Worker2> logger;
    private readonly IEventStore eventStore;

    public Worker2(ILogger<Worker2> logger, IEventStore eventStore)
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
            logger.LogInformation("Worker2 Tick");
            await Task.Delay(10000, stoppingToken);
        }
    }

    private void Handler(Event<SomethingHappened> e)
    {
        logger.LogInformation($"Received: {e.EventModel.Foo}");

        eventStore.Produce(new ThenThisHappened("bar bar"), "topic2").GetAwaiter().GetResult();
    }
}
