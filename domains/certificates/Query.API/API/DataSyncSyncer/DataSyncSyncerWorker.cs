using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Service;
using API.DataSyncSyncer.Service.IntegrationService;
using API.MasterDataService;
using CertificateEvents;
using EnergyOriginEventStore.EventStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly IEventStore eventStore;
    private readonly string? gsrn;
    private readonly IAwesomeQueue awesomeQueue;
    private readonly IDataSyncProvider dataSyncProvider;

    public DataSyncSyncerWorker(
        ILogger<DataSyncSyncerWorker> logger,
        IEventStore eventStore,
        MockMasterDataCollection collection,
        IAwesomeQueue queue,
        IDataSyncProvider dataSyncProvider
        )
    {
        this.logger = logger;
        this.eventStore = eventStore;
        var masterData = collection.Data.FirstOrDefault();
        awesomeQueue = queue;
        this.dataSyncProvider = dataSyncProvider;
        gsrn = masterData?.GSRN ?? null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(gsrn))
        {
            logger.LogWarning("No master data loaded. Will not produce any events");
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500),
            stoppingToken); //allow application to start before producing events
        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            await ShipTheMasterData(stoppingToken);

            if (!string.IsNullOrWhiteSpace(gsrn))
            {
                logger.LogInformation("Produce energy measured event");

                var @event = new EnergyMeasured(gsrn, new(42, 50), random.NextInt64(1, 42),
                    EnergyMeasurementQuality.Measured);
                await eventStore.Produce(@event, Topic.For(@event));
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ShipTheMasterData(CancellationToken cancellationToken)
    {
        await awesomeQueue.Produce(cancellationToken, await dataSyncProvider.GetMasterData(gsrn));
    }
}
