using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Issuer.Worker.DataSyncSyncer;

public class DataSyncSyncerWorker : BackgroundService
{
    private readonly ILogger<DataSyncSyncerWorker> _logger;
    private readonly IEventStore _eventStore;

    public DataSyncSyncerWorker(ILogger<DataSyncSyncerWorker> logger, IEventStore eventStore)
    {
        _logger = logger;
        _eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Produce energy measured event");

            var @event = new EnergyMeasured("gsrn", new(42, 50), 42, EnergyMeasurementQuality.Measured);
            await _eventStore.Produce(@event, Topic.For(@event));

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
