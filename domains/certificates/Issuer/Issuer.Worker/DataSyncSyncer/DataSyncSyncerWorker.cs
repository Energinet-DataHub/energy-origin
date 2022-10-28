using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents;
using EnergyOriginEventStore.EventStore;
using Issuer.Worker.MasterDataService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Issuer.Worker.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly ILogger<DataSyncSyncerWorker> _logger;
    private readonly IEventStore _eventStore;
    private readonly string? _gsrn;

    public DataSyncSyncerWorker(ILogger<DataSyncSyncerWorker> logger, IEventStore eventStore,
        MockMasterDataCollection collection)
    {
        _logger = logger;
        _eventStore = eventStore;
        var masterData = collection.Data.FirstOrDefault();
        _gsrn = masterData?.GSRN ?? null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_gsrn))
        {
            _logger.LogWarning("No master data loaded. Will not produce any events");
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500),
            stoppingToken); //allow application to start before producing events
        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(_gsrn))
            {
                _logger.LogInformation("Produce energy measured event");

                var @event = new EnergyMeasured(_gsrn, new(42, 50), random.NextInt64(1, 42),
                    EnergyMeasurementQuality.Measured);
                await _eventStore.Produce(@event, Topic.For(@event));
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
