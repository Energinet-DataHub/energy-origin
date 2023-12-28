using System;
using DataContext.ValueObjects;

namespace Contracts.Certificates.CertificateRejectedInRegistry.V1;

public record CertificateRejectedInRegistryEvent(Guid CertificateId, MeteringPointType MeteringPointType, string Reason);
