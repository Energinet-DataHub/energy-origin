using System;
using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

public abstract record CertificateBaseEvent : EventModel
{
    public abstract Guid CertificateId { get; init; }
}
