using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

internal class DummyEnergyMeasProducer : BackgroundService
{
    private readonly IBus bus;
    private readonly ILogger<DummyEnergyMeasProducer> logger;
    private readonly string? gsrn;

    public DummyEnergyMeasProducer(IBus bus, MockMasterDataCollection collection, ILogger<DummyEnergyMeasProducer> logger)
    {
        this.bus = bus;
        this.logger = logger;
        var masterData = collection.Data.FirstOrDefault();
        gsrn = masterData?.GSRN ?? null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(gsrn))
            return;

        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            var measurement = new Measurement(gsrn, new Period(now.AddHours(-1).ToUnixTimeSeconds(), now.ToUnixTimeSeconds()), random.NextInt64(1, 42), EnergyMeasurementQuality.Measured);
            await bus.Publish(measurement, stoppingToken);

            logger.LogInformation("publish");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

public record Measurement(
    string GSRN,
    Period Period,
    long Quantity,
    EnergyMeasurementQuality Quality
);
