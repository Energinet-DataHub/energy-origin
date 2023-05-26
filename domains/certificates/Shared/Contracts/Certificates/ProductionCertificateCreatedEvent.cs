using System;
using CertificateValueObjects;

namespace Contracts.Certificates;

public record ProductionCertificateCreatedEvent(
    Guid CertificateId,
    string GridArea,
    Period Period,
    Technology Technology,
    string MeteringPointOwner,
    ShieldedValue<Gsrn> ShieldedGsrn,
    ShieldedValue<long> ShieldedQuantity
);
