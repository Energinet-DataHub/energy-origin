using System;
using CertificateEvents.Primitives;

namespace CertificateEvents;

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
