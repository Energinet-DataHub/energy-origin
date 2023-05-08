using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace RegistryConnector.Worker;

public class Worker : BackgroundService
{
    private readonly IBus bus;

    public Worker(IBus bus)
    {
        this.bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //while (!stoppingToken.IsCancellationRequested)
        //{
        //    await bus.Publish(new ProductionCertificateCreatedEvent(Guid.NewGuid(),
        //        "DK1",
        //        new Period(
        //            new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        //            new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()),
        //        new Technology(FuelCode: "F00000000", TechCode: "T070000"),
        //        "SomeMeteringPointOwner",
        //        new ShieldedValue<Gsrn>(new Gsrn("57000001234567"), BigInteger.Zero),
        //        new ShieldedValue<long>(150, BigInteger.Zero)));

        //    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        //}
    }
}
