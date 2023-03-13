using System;

namespace CertificateEvents;


public record ProductionCertificateRejected(Guid CertificateId, string Reason, string GSRN);
