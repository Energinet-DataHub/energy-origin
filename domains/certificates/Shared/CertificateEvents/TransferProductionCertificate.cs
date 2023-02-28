using System;

namespace CertificateEvents;

public record TransferProductionCertificate(
    string CurrentOwner,
    string NewOwner,
    Guid CertificateId
    );
