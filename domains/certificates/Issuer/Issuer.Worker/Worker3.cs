using EnergyOriginEventStore.EventStore;
using Issuer.Worker;

public class Worker3 : BackgroundService
{
    private readonly ILogger<Worker3> logger;
    private readonly IEventStore eventStore;

    public Worker3(ILogger<Worker3> logger, IEventStore eventStore)
    {
        this.logger = logger;
        this.eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = eventStore
            .GetBuilder("topic2")
            .AddHandler<ThenThisHappened>(e => logger.LogInformation($"Then this happened: {e.EventModel.Bar}"))
            .Build();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            //logger.LogInformation("Worker3 Tick");
            await Task.Delay(1000, stoppingToken);
        }
    }
}
