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
    private readonly ILogger<QueryModelUpdaterWorker> _logger;
    private readonly IEventStore _eventStore;

    public QueryModelUpdaterWorker(ILogger<QueryModelUpdaterWorker> logger, IEventStore eventStore)
    {
        _logger = logger;
        _eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = _eventStore
            .GetBuilder(Topic.CertificatePrefix)
            .AddHandler<ProductionCertificateCreated>(e => _logger.LogInformation("QueryModelUpdaterWorker received: {event}", e.EventModel))
            .AddHandler<ProductionCertificateIssued>(e => _logger.LogInformation("QueryModelUpdaterWorker received: {event}", e.EventModel))
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
