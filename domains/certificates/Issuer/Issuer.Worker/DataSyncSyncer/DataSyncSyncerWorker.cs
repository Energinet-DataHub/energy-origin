using EnergyOriginEventStore.EventStore;

namespace Issuer.Worker.DataSyncSyncer;

public class DataSyncSyncerWorker : BackgroundService
{
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly IEventStore eventStore;

    public DataSyncSyncerWorker(ILogger<DataSyncSyncerWorker> logger, IEventStore eventStore)
    {
        this.logger = logger;
        this.eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            //logger.LogInformation("Worker: {time}", DateTimeOffset.Now);
            await eventStore.Produce(new SomethingHappened(DateTimeOffset.Now.ToString()), "topic1");

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
