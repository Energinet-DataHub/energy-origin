using System;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificatesEvents;

[EventModelVersion("CertificateIssued", 1)]
public record CertificateIssued(Guid CertificateId) : EventModel;
