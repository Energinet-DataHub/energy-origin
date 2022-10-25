using System;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

[EventModelVersion("CertificateRejected", 1)]
public record CertificateRejected(Guid CertificateId, string Reason) : CertificateBaseEvent;
