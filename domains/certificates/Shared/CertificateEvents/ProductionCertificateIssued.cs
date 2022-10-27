using System;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

[EventModelVersion("ProductionCertificateIssued", 1)]
public record ProductionCertificateIssued(Guid CertificateId) : CertificateBaseEvent;
