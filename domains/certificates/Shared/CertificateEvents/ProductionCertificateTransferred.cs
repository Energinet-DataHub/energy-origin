using System;

namespace CertificateEvents;

public record ProductionCertificateTransferred(
    Guid CertificateId,
    string Source,
    string Target
);
