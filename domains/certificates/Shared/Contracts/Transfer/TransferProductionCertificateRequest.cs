using System;

namespace Contracts.Transfer;

public record TransferProductionCertificateRequest(
    string CurrentOwner,
    string NewOwner,
    Guid CertificateId
    );
