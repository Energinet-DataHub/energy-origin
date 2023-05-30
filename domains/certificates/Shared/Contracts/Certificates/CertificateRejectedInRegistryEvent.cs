using System;

namespace Contracts.Certificates;

public record CertificateRejectedInRegistryEvent(Guid CertificateId, string Reason);
