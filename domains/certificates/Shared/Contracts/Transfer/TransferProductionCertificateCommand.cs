using System;

namespace Contracts.Transfer;

public record TransferProductionCertificateCommand(string Source, string Target, Guid CertificateId);
