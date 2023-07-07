using System;
using System.Numerics;
using CertificateValueObjects;
using Contracts.Certificates;

namespace RegistryConnector.Worker.UnitTests;

public static class Some
{
    public static readonly ProductionCertificateCreatedEvent ProductionCertificateCreatedEvent =
        new(Guid.NewGuid(),
            "DK1",
            new Period(DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.AddMinutes(15).ToUnixTimeSeconds()),
            new Technology(FuelCode: "F00000000", TechCode: "T070000"),
            "SomeMeteringPointOwner",
            new ShieldedValue<Gsrn>(new Gsrn("571234567890123456"), BigInteger.Zero),
            new ShieldedValue<long>(42, BigInteger.Zero));
}
