using System;

namespace CertificateEvents;

public record ProductionCertificateIssued(Guid CertificateId, string MeteringPointOwner, string GSRN);
