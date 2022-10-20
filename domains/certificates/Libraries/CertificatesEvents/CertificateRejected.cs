using System;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificatesEvents;

[EventModelVersion("CertificateRejected", 1)]
public record CertificateRejected(Guid CertificateId, string Reason) : EventModel;
