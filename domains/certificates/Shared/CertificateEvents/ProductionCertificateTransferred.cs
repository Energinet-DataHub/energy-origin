using System;
using CertificateEvents.Primitives;

namespace CertificateEvents;

public record ProductionCertificateTransferred(
    Guid CertificateId,
    string Source,
    string Target
);
