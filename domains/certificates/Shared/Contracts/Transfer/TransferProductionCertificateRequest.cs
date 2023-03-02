using System;

namespace Contracts.Transfer;

public record TransferProductionCertificateRequest(string Source, string Target, Guid CertificateId);
