using System;
using DomainCertificate;
using DomainCertificate.Primitives;
using DomainCertificate.ValueObjects;

namespace CertificateEvents;

public record ProductionCertificateCreated(
    Guid CertificateId,
    string GridArea,
    Period Period,
    Technology Technology,
    string MeteringPointOwner,
    ShieldedValue<string> ShieldedGSRN,
    ShieldedValue<long> ShieldedQuantity
);
