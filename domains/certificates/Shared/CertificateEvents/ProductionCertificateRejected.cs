using System;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

[EventModelVersion("ProductionCertificateRejected", 1)]
public record ProductionCertificateRejected(Guid CertificateId, string Reason) : CertificateBaseEvent;
