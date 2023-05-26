using System;

namespace Contracts.Certificates;

public record CertificateIssuedInRegistryEvent(Guid CertificateId);
