using EnergyOriginEventStore.EventStore;

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
            .GetBuilder("topic2")
            .AddHandler<ThenThisHappened>(e => logger.LogInformation($"Then this happened: {e.EventModel.Bar}"))
            .Build();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            //logger.LogInformation("Worker Tick");
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
