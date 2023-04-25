using System;
using Domain;
using Domain.Certificates.Primitives;

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
