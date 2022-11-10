using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents.Primitives;
using CertificateEvents;
using MassTransit;
using Microsoft.Extensions.Hosting;
using API.MasterDataService;
using System.Linq;

namespace API.GranularCertificateIssuer;

internal class DummyEnergyMeasProducer : BackgroundService
{
    private readonly IBus bus;
    private readonly string? gsrn;

    public DummyEnergyMeasProducer(IBus bus, MockMasterDataCollection collection)
    {
        this.bus = bus;
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
            var measurement = new Measurement(gsrn, new Period(1, 42), random.NextInt64(1, 42), EnergyMeasurementQuality.Measured);
            await bus.Publish(measurement, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}

public record Measurement(
    string GSRN,
    Period Period,
    long Quantity,
    EnergyMeasurementQuality Quality
);
