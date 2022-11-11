using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MasterDataService;
using IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.DataSyncSyncer;

internal class DataSyncSyncerWorker : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<DataSyncSyncerWorker> logger;
    private readonly string? gsrn;

    public DataSyncSyncerWorker(IBus bus, MockMasterDataCollection collection, ILogger<DataSyncSyncerWorker> logger)
    {
        this.bus = bus;
        this.logger = logger;
        var masterData = collection.Data.FirstOrDefault();
        gsrn = masterData?.GSRN ?? null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(gsrn))
        {
            logger.LogWarning("No master data loaded. Will not produce any events");
            return;
        }

        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            var measurement = new EnergyMeasuredIntegrationEvent(gsrn, now.AddHours(-1).ToUnixTimeSeconds(), now.ToUnixTimeSeconds(), random.NextInt64(1, 42), MeasurementQuality.Measured);
            await bus.Publish(measurement, stoppingToken);

            logger.LogInformation("Publish EnergyMeasuredIntegrationEvent");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
