using System;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

[EventModelVersion("CertificateIssued", 1)]
public record CertificateIssued(Guid CertificateId) : EventModel;
