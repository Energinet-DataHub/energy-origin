using System;
using System.Numerics;
using Contracts.Certificates;
using DomainCertificate;
using DomainCertificate.Primitives;
using DomainCertificate.ValueObjects;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.UnitTests
{
    public static class Some
    {
        private static readonly byte[] commandIdHash = { 26, 29, 31, 227, 125, 21, 41, 199, 11, 127, 63, 88, 214, 204, 167, 243, 177, 165, 242, 43, 127, 154, 47, 116, 12, 230, 13, 8, 69, 227, 43 };

        public static readonly CommandId CommandId = new(commandIdHash);

        public static readonly ProductionCertificateCreatedEvent ProductionCertificateCreatedEvent =
            new(Guid.NewGuid(),
                "DK1",
                new Period(DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.AddMinutes(15).ToUnixTimeSeconds()),
                new Technology(FuelCode: "F00000000", TechCode: "T070000"),
                "SomeMeteringPointOwner",
                new ShieldedValue<Gsrn>(new Gsrn("111111111111111111"), BigInteger.Zero),
                new ShieldedValue<long>(42, BigInteger.Zero));
    }
}
