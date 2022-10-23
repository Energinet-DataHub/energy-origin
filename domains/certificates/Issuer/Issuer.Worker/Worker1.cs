using EnergyOriginEventStore.EventStore;

namespace Issuer.Worker;

public class Worker1 : BackgroundService
{
    private readonly ILogger<Worker1> logger;
    private readonly IEventStore eventStore;

    public Worker1(ILogger<Worker1> logger, IEventStore eventStore)
    {
        this.logger = logger;
        this.eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            //logger.LogInformation("Worker1: {time}", DateTimeOffset.Now);
            await eventStore.Produce(new SomethingHappened(DateTimeOffset.Now.ToString()), "topic1");
            await Task.Delay(1000, stoppingToken);
        }
    }
}
