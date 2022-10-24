using System;
using System.Threading;
using System.Threading.Tasks;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Issuer.Worker.QueryModelUpdater;

public class QueryModelUpdaterWorker : BackgroundService
{
    private readonly ILogger<QueryModelUpdaterWorker> logger;
    private readonly IEventStore eventStore;

    public QueryModelUpdaterWorker(ILogger<QueryModelUpdaterWorker> logger, IEventStore eventStore)
    {
        this.logger = logger;
        this.eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            //logger.LogInformation("Worker Tick");
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
