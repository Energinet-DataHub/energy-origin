using System;
using CertificateEvents.Primitives;

namespace CertificateEvents;

public record ProductionCertificateTransferred(
    Guid CertificateId,
    string Source,
    string Target

    // Duplicated values from below
    // string GridArea,
    // Period Period,
    // Technology Technology,
    // ShieldedValue<string> ShieldedGSRN,
    // ShieldedValue<long> ShieldedQuantity
);
