using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.QueryModelUpdater;

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
        using var consumer = eventStore
            .GetBuilder(Topic.CertificatePrefix)
            .AddHandler<ProductionCertificateCreated>(e => logger.LogInformation("QueryModelUpdaterWorker received: {event}", e.EventModel))
            .AddHandler<ProductionCertificateIssued>(e => logger.LogInformation("QueryModelUpdaterWorker received: {event}", e.EventModel))
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
