using System;
using DomainCertificate;
using DomainCertificate.Primitives;
using DomainCertificate.ValueObjects;

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