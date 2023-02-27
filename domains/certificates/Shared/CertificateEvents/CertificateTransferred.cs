using System;
using CertificateEvents.Primitives;

namespace CertificateEvents;

//TODO: Should this be renamed to ProductionCertificateTransferred to make it consistent with the rest of the events?
public record CertificateTransferred(
    Guid CertificateId,
    string From,
    string To,

    // Duplicated values from below
    string GridArea,
    Period Period,
    Technology Technology,
    ShieldedValue<string> ShieldedGSRN,
    ShieldedValue<long> ShieldedQuantity
);
